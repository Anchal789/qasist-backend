using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Execution.Assertions
{
    /// <summary>
    /// T22 + T30 — AssertionEngine: facade that dispatches to the correct
    /// IAssertor implementation via a factory lookup.
    ///
    /// All IAssertor implementations are registered in DI and injected here.
    /// Adding a new assertion type = create new IAssertor class + register in DI.
    /// Nothing in AssertionEngine changes (Open/Closed Principle).
    ///
    /// CRITICAL RULE: EvaluateAll() NEVER throws.
    /// Any exception from an assertor is caught and returned as a
    /// failed AssertionResult with an error message.
    /// </summary>
    public class AssertionEngine : IAssertionEngine
    {
        private readonly IReadOnlyDictionary<AssertionType, IAssertor> _assertors;
        private readonly ILogger<AssertionEngine> _logger;

        public AssertionEngine(
            IEnumerable<IAssertor> assertors,
            ILogger<AssertionEngine> logger)
        {
            // Build lookup dictionary from all registered IAssertor implementations
            _assertors = assertors.ToDictionary(a => a.SupportedType);
            _logger = logger;
        }

        public List<AssertionResult> EvaluateAll(
            IEnumerable<Domain.Entities.Assertion> assertions,
            HttpStepResponse response,
            long durationMs)
        {
            var results = new List<AssertionResult>();
            var assertionList = assertions?.ToList() ?? new List<Domain.Entities.Assertion>();

            if (assertionList.Count == 0)
            {
                _logger.LogDebug("No assertions to evaluate for this step.");
                return results;
            }

            _logger.LogDebug("Evaluating {Count} assertions.", assertionList.Count);

            foreach (var assertion in assertionList.OrderBy(a => a.OrderIndex))
            {
                AssertionResult result;

                try
                {
                    // Look up the correct assertor for this assertion type
                    if (!_assertors.TryGetValue(assertion.AssertionType, out var assertor))
                    {
                        _logger.LogWarning(
                            "No assertor registered for type {Type}. Skipping.",
                            assertion.AssertionType);

                        result = AssertionResult.Error(
                            assertion.Id,
                            assertion.AssertionType,
                            $"No assertor registered for type '{assertion.AssertionType}'");
                    }
                    else
                    {
                        result = assertor.Evaluate(assertion, response, durationMs);
                    }
                }
                catch (Exception ex)
                {
                    // Individual assertor threw — log and continue, never abort
                    _logger.LogError(ex,
                        "Assertor {Type} threw an unexpected exception for assertion {Id}.",
                        assertion.AssertionType, assertion.Id);

                    result = AssertionResult.Error(
                        assertion.Id,
                        assertion.AssertionType,
                        ex.Message);
                }

                results.Add(result);

                _logger.LogDebug(
                    "Assertion {Type}: {Status} | {Message}",
                    assertion.AssertionType,
                    result.Passed ? "PASSED" : "FAILED",
                    result.Message);
            }

            var passed = results.Count(r => r.Passed);
            var failed = results.Count(r => !r.Passed);

            _logger.LogInformation(
                "Assertion evaluation complete. Passed={Passed}, Failed={Failed}",
                passed, failed);

            return results;
        }
    }
}
