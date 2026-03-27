using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Execution.Assertions.Assertors
{
    /// <summary>
    /// T23 — Validates that the HTTP response status code
    /// matches the expected value.
    ///
    /// Most common assertion — almost every test step has this.
    ///
    /// Example:
    ///   Expected: "200"  Actual: 200  → Passed
    ///   Expected: "201"  Actual: 200  → Failed
    ///   Expected: "abc"  Actual: 200  → Failed (invalid expected, descriptive error)
    /// </summary>
    public class StatusCodeAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.StatusCodeEquals;

        public AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs)
        {
            // Validate expected value is a valid integer
            if (!int.TryParse(assertion.ExpectedValue, out var expectedCode))
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Invalid expected status code: '{assertion.ExpectedValue}'. Must be a number.",
                    actualValue: response.StatusCode.ToString(),
                    expectedValue: assertion.ExpectedValue,
                    isRequired: assertion.IsRequired);
            }

            var passed = response.StatusCode == expectedCode;

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Status code {response.StatusCode} matches expected {expectedCode}.",
                    actualValue: response.StatusCode.ToString(),
                    expectedValue: expectedCode.ToString())
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Expected status code {expectedCode} but got {response.StatusCode}.",
                    actualValue: response.StatusCode.ToString(),
                    expectedValue: expectedCode.ToString(),
                    isRequired: assertion.IsRequired);
        }
    }
}
