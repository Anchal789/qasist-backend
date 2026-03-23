using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QAsist.Application.Execution.Assertions.Assertors
{
    /// Edge cases handled:
    ///   - Invalid regex pattern → Failed with descriptive message (no throw)
    ///   - Field not found       → Failed
    ///   - Null field value      → Treated as empty string for match
    ///
    /// Example:
    ///   Field: "$.data.email"
    ///   Expected: "^[^@]+@[^@]+\.[^@]+$"
    ///   Body: {"data":{"email":"test@test.com"}}
    ///   → Extracts "test@test.com", regex matches → Passed
    /// </summary>
    public class FieldMatchesRegexAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.FieldMatchesRegex;

        public AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs)
        {
            // Validate regex pattern first
            if (string.IsNullOrWhiteSpace(assertion.ExpectedValue))
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    "Regex pattern (ExpectedValue) is empty or null.",
                    isRequired: assertion.IsRequired);
            }

            // Validate the regex pattern is syntactically valid
            try
            {
                _ = new Regex(assertion.ExpectedValue);
            }
            catch (ArgumentException ex)
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Invalid regex pattern '{assertion.ExpectedValue}': {ex.Message}",
                    isRequired: assertion.IsRequired);
            }

            // Extract field value via JSONPath
            var token = JsonPathHelper.SelectToken(
                response.Body,
                assertion.Field,
                out var errorMessage);

            if (errorMessage is not null)
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    errorMessage,
                    isRequired: assertion.IsRequired);
            }

            if (token is null)
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' not found in response body.",
                    isRequired: assertion.IsRequired);
            }

            var actualValue = JsonPathHelper.TokenToString(token) ?? string.Empty;

            // Evaluate the regex
            var passed = Regex.IsMatch(actualValue, assertion.ExpectedValue);

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' value '{actualValue}' matches pattern '{assertion.ExpectedValue}'.",
                    actualValue: actualValue,
                    expectedValue: assertion.ExpectedValue)
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' value '{actualValue}' does not match pattern '{assertion.ExpectedValue}'.",
                    actualValue: actualValue,
                    expectedValue: assertion.ExpectedValue,
                    isRequired: assertion.IsRequired);
        }
    }
}
