using QAsist.Domain.Entities;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Interfaces.IContext
{
    public interface IStepExecutor
    {
        /// <summary>
        /// Executes one step and returns its full result.
        /// Never throws — all exceptions captured in StepExecutionResult.
        /// </summary>
        Task<StepExecutionResult> ExecuteAsync(
            TestStep step,
            IExecutionContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Week 7 — Executes an entire TestSuite.
    /// Orchestrates: create context → seed env vars → run cases → aggregate.
    /// </summary>
    public interface ISuiteExecutor
    {
        /// <summary>
        /// Runs all cases and steps in the suite.
        /// Returns execution summary with pass/fail counts.
        /// </summary>
        Task<SuiteExecutionResult> ExecuteAsync(
            TestSuite suite,
            ExecutionOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Options passed when executing a suite.</summary>
    public sealed class ExecutionOptions
    {
        public Guid BatchId { get; init; } = Guid.NewGuid();
        public Guid TenantId { get; init; }
        public Guid EnvironmentId { get; init; }
        public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
        public bool FailFast { get; init; } = false;
        public bool ParallelCases { get; init; } = false;
    }

    /// <summary>Final result returned after a suite completes.</summary>
    public sealed class SuiteExecutionResult
    {
        public Guid BatchId { get; init; }
        public Guid SuiteId { get; init; }
        public int TotalSteps { get; init; }
        public int PassedSteps { get; init; }
        public int FailedSteps { get; init; }
        public int SkippedSteps { get; init; }
        public int ErrorSteps { get; init; }
        public long TotalDurationMs { get; init; }
        public List<StepExecutionResult> StepResults { get; init; } = new();

        public double PassPercentage =>
            TotalSteps == 0 ? 0
            : Math.Round((double)PassedSteps / TotalSteps * 100, 2);

        public bool IsSuccess => FailedSteps == 0 && ErrorSteps == 0;
    }
}
