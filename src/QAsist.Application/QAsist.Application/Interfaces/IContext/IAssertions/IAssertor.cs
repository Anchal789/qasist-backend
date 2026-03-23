using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Interfaces.IContext.IAssertions
{
    /// <summary>
    /// T22a — Contract for a single assertion evaluator (Strategy Pattern).
    /// Each AssertionType has exactly ONE implementation of this interface.
    /// New assertion types = new class implementing IAssertor. Nothing else changes.
    /// </summary>
    public interface IAssertor
    {
        /// <summary>The assertion type this evaluator handles.</summary>
        AssertionType SupportedType { get; }

        /// <summary>
        /// Evaluates a single assertion against the HTTP response.
        /// NEVER throws — all exceptions caught and returned as failed result.
        /// </summary>
        /// <param name="assertion">The assertion rule to evaluate.</param>
        /// <param name="response">The HTTP response to validate.</param>
        /// <param name="durationMs">Response time in milliseconds.</param>
        AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs);
    }
}
