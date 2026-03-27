using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Execution.Assertions.Assertors
{
    /// <summary>
    /// T24 — Validates that the HTTP response time is strictly
    /// less than the expected threshold in milliseconds.
    ///
    /// IMPORTANT: Strictly LESS THAN — not less-than-or-equal.
    /// Expected="500", actual=500 → FAILED
    /// Expected="500", actual=499 → PASSED
    ///
    /// Example:
    ///   Expected: "500"  Actual: 200ms  → Passed
    ///   Expected: "500"  Actual: 600ms  → Failed
    ///   Expected: "500"  Actual: 500ms  → Failed (boundary: strictly less than)
    /// </summary>
    public class ResponseTimeAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.ResponseTimeLessThan;

        public AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs)
        {
            // Validate expected value is a valid long (milliseconds)
            if (!long.TryParse(assertion.ExpectedValue, out var thresholdMs))
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Invalid response time threshold: '{assertion.ExpectedValue}'. Must be a number (milliseconds).",
                    actualValue: durationMs.ToString(),
                    expectedValue: assertion.ExpectedValue,
                    isRequired: assertion.IsRequired);
            }

            // Strictly less than (not <=)
            var passed = durationMs < thresholdMs;

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Response time {durationMs}ms is less than threshold {thresholdMs}ms.",
                    actualValue: $"{durationMs}ms",
                    expectedValue: $"< {thresholdMs}ms")
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Response time {durationMs}ms exceeds threshold {thresholdMs}ms.",
                    actualValue: $"{durationMs}ms",
                    expectedValue: $"< {thresholdMs}ms",
                    isRequired: assertion.IsRequired);
        }
    }
}
