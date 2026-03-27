using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Execution.Assertions.Assertors
{
    /// <summary>
    /// T25 — Validates that the response body contains
    /// the expected substring (case-insensitive).
    ///
    /// Edge cases handled:
    ///   - Null body        → Passed=false, no exception
    ///   - Empty body       → Passed=false
    ///   - Empty expected   → Passed=true (empty string always contained)
    ///   - Case-insensitive → "SUCCESS" matches "success"
    ///
    /// Example:
    ///   Expected: "success"   Body: '{"status":"success"}'  → Passed
    ///   Expected: "SUCCESS"   Body: '{"status":"success"}'  → Passed (case-insensitive)
    ///   Expected: "error"     Body: '{"status":"success"}'  → Failed
    ///   Expected: "text"      Body: null                    → Failed
    /// </summary>
    public class BodyContainsAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.BodyContains;

        public AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs)
        {
            var expected = assertion.ExpectedValue ?? string.Empty;

            // Null/empty body guard — never throw NullReferenceException
            if (string.IsNullOrEmpty(response.Body))
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Response body is empty or null. Expected to contain: '{expected}'.",
                    actualValue: "[empty body]",
                    expectedValue: expected,
                    isRequired: assertion.IsRequired);
            }

            // Case-insensitive contains check
            var passed = response.Body.Contains(
                expected,
                StringComparison.OrdinalIgnoreCase);

            // Truncate body in message if too long for readability
            var bodyPreview = response.Body.Length > 200
                ? response.Body[..200] + "..."
                : response.Body;

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Response body contains '{expected}'.",
                    actualValue: bodyPreview,
                    expectedValue: expected)
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Response body does not contain '{expected}'.",
                    actualValue: bodyPreview,
                    expectedValue: expected,
                    isRequired: assertion.IsRequired);
        }
    }
}
