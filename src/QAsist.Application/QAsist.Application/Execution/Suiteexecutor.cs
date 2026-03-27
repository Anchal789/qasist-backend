using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QAsist.Application.Execution
{
    /// Flow:
    ///   1. Create ExecutionContext
    ///   2. Seed with environment variables
    ///   3. Iterate TestCases (sequential or parallel)
    ///   4. For each case, iterate TestSteps via StepExecutor
    ///   5. Handle failFast: abort remaining steps if one fails
    ///   6. Aggregate results into SuiteExecutionResult
    ///
    /// Auth priority (Week 7):
    ///   Step.AuthConfig overrides Environment auth.
    ///   Resolved by AuthInjector.ResolveAuthConfig().
    /// </summary>
    public class SuiteExecutor : ISuiteExecutor
    {
        private readonly IStepExecutor _stepExecutor;
        private readonly ILogger<SuiteExecutor> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public SuiteExecutor(
            IStepExecutor stepExecutor,
            ILoggerFactory loggerFactory,
            ILogger<SuiteExecutor> logger)
        {
            _stepExecutor = stepExecutor;
            _loggerFactory = loggerFactory;
            _logger = logger;
        }

        public async Task<SuiteExecutionResult> ExecuteAsync(
            TestSuite suite,
            ExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting suite '{Name}' (BatchId={BatchId}, FailFast={FF}, Parallel={P})",
                suite.Name, options.BatchId, options.FailFast, options.ParallelCases);

            // ── 1. Create ExecutionContext ─────────────────────────────────────
            var context = new ExecutionContext(
                tenantId: options.TenantId,
                projectId: suite.ProjectId,
                suiteId: suite.Id,
                environmentId: options.EnvironmentId,
                batchId: options.BatchId,
                logger: _loggerFactory.CreateLogger("ExecutionContext"));

            // ── 2. Seed suite variables first (lower priority) ────────────────
            foreach (var (key, value) in suite.Variables)
                context.SetVariable(key, value);

            // ── 3. Merge environment variables (does NOT overwrite suite vars) ─
            context.MergeEnvironmentVariables(options.EnvironmentVariables);

            _logger.LogDebug(
                "Context seeded with {Count} variables.",
                context.GetAllVariables().Count);

            // ── 4. Execute test cases ─────────────────────────────────────────
            var allResults = new List<StepExecutionResult>();
            var activeCases = suite.TestCases?
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.OrderIndex)
                .ToList() ?? new List<TestCaseSuite>();

            if (options.ParallelCases)
                allResults = await ExecuteParallelAsync(
                    activeCases, context, options, cancellationToken);
            else
                allResults = await ExecuteSequentialAsync(
                    activeCases, context, options, cancellationToken);

            sw.Stop();

            // ── 5. Aggregate ──────────────────────────────────────────────────
            var result = new SuiteExecutionResult
            {
                BatchId = options.BatchId,
                SuiteId = suite.Id,
                TotalSteps = allResults.Count,
                PassedSteps = allResults.Count(r => r.Status == StepStatus.Passed),
                FailedSteps = allResults.Count(r => r.Status == StepStatus.Failed),
                SkippedSteps = allResults.Count(r => r.Status == StepStatus.Skipped),
                ErrorSteps = allResults.Count(r => r.Status == StepStatus.Error),
                TotalDurationMs = sw.ElapsedMilliseconds,
                StepResults = allResults
            };

            _logger.LogInformation(
                "Suite '{Name}' complete. Passed={P} Failed={F} Skipped={S} Error={E} " +
                "Pass%={Pct}% Duration={Ms}ms",
                suite.Name,
                result.PassedSteps, result.FailedSteps,
                result.SkippedSteps, result.ErrorSteps,
                result.PassPercentage, result.TotalDurationMs);

            return result;
        }

        // ── Sequential execution ──────────────────────────────────────────────
        private async Task<List<StepExecutionResult>> ExecuteSequentialAsync(
            List<TestCaseSuite> cases,
            IExecutionContext context,
            ExecutionOptions options,
            CancellationToken cancellationToken)
        {
            var allResults = new List<StepExecutionResult>();
            bool aborted = false;

            foreach (var testCase in cases)
            {
                if (aborted || cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogDebug(
                    "Executing case '{Name}' (OrderIndex={Idx})",
                    testCase.Name, testCase.OrderIndex);

                var caseResults = await ExecuteCaseAsync(
                    testCase, context, options, cancellationToken);

                allResults.AddRange(caseResults);

                // FailFast: stop if any required assertion failed
                if (options.FailFast &&
                    caseResults.Any(r =>
                        r.Status == StepStatus.Failed ||
                        r.Status == StepStatus.Error))
                {
                    _logger.LogWarning(
                        "FailFast triggered after case '{Name}'. Aborting suite.",
                        testCase.Name);
                    aborted = true;
                }
            }

            return allResults;
        }

        // ── Parallel execution ────────────────────────────────────────────────
        private async Task<List<StepExecutionResult>> ExecuteParallelAsync(
            List<TestCaseSuite> cases,
            IExecutionContext context,
            ExecutionOptions options,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Running {Count} cases in parallel.", cases.Count);

            // NOTE: Parallel cases share the same context (ConcurrentDictionary)
            // Variables extracted in one case ARE visible to concurrent cases
            // but ordering is non-deterministic. Use sequential for chained suites.
            var tasks = cases.Select(c =>
                ExecuteCaseAsync(c, context, options, cancellationToken));

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList();
        }

        // ── Execute one case (all its steps) ──────────────────────────────────
        private async Task<List<StepExecutionResult>> ExecuteCaseAsync(
            TestCaseSuite testCase,
            IExecutionContext context,
            ExecutionOptions options,
            CancellationToken cancellationToken)
        {
            var results = new List<StepExecutionResult>();
            bool aborted = false;

            var activeSteps = testCase.Steps?
                .Where(s => s.IsEnabled)
                .OrderBy(s => s.OrderIndex)
                .ToList() ?? new List<TestStep>();

            foreach (var step in activeSteps)
            {
                if (aborted || cancellationToken.IsCancellationRequested)
                {
                    // Mark remaining steps as Skipped
                    results.Add(new StepExecutionResult
                    {
                        TestStepId = step.Id,
                        Status = StepStatus.Skipped,
                        DurationMs = 0,
                        ErrorMessage = "Skipped due to FailFast."
                    });
                    continue;
                }

                var stepResult = await _stepExecutor.ExecuteAsync(
                    step, context, cancellationToken);

                results.Add(stepResult);

                // FailFast check after each step
                if (options.FailFast &&
                    (stepResult.Status == StepStatus.Failed ||
                     stepResult.Status == StepStatus.Error))
                {
                    _logger.LogWarning(
                        "FailFast: step '{StepId}' failed. Skipping remaining steps " +
                        "in case '{CaseName}'.",
                        step.Id, testCase.Name);
                    aborted = true;
                }
            }

            return results;
        }
    }
}
