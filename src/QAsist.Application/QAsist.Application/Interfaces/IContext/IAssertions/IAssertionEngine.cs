using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Interfaces.IContext.IAssertions
{
    /// <summary>
    /// T22b — Facade that runs all assertions for a step.
    /// Returns ALL results — never stops at first failure.
    /// Caller (StepExecutor) decides pass/fail based on results.
    /// </summary>
    public interface IAssertionEngine
    {
        /// <summary>
        /// Evaluates all assertions for a step.
        /// Returns one AssertionResult per assertion.
        /// Never throws — individual assertor exceptions returned as error results.
        /// </summary>
        List<AssertionResult> EvaluateAll(
            IEnumerable<Domain.Entities.Assertion> assertions,
            HttpStepResponse response,
            long durationMs);
    }
}
