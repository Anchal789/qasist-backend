using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QAsist.Application.Execution.Assertions.Assertors
{
    /// Comparison is string-based (both sides converted to string).
    /// Case-sensitive by default.
    ///
    /// Edge cases handled:
    ///   - Invalid JSON body   → Failed with descriptive message
    ///   - Invalid JSONPath    → Failed with descriptive message
    ///   - Path not found      → Failed (field does not exist)
    ///   - Array result        → First element used for comparison
    ///   - Null token value    → compared as empty string ""
    ///
    /// Example:
    ///   Field: "$.data.id"  Expected: "{{userId}}"  Body: {"data":{"id":"abc"}}
    ///   → Extracts "abc", compares to "abc" → Passed
    /// </summary>
    public class FieldEqualsAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.FieldEquals;

        public AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs)
        {
            var token = JsonPathHelper.SelectToken(
                response.Body,
                assertion.Field,
                out var errorMessage);

            // JSONPath evaluation error (invalid JSON or invalid path)
            if (errorMessage is not null)
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    errorMessage,
                    actualValue: "[error]",
                    expectedValue: assertion.ExpectedValue,
                    isRequired: assertion.IsRequired);
            }

            // Path not found in JSON
            if (token is null)
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' not found in response body.",
                    actualValue: "[not found]",
                    expectedValue: assertion.ExpectedValue,
                    isRequired: assertion.IsRequired);
            }

            var actualValue = JsonPathHelper.TokenToString(token) ?? string.Empty;
            var expectedValue = assertion.ExpectedValue ?? string.Empty;
            var passed = string.Equals(actualValue, expectedValue,
                                    StringComparison.Ordinal);

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' equals '{expectedValue}'.",
                    actualValue: actualValue,
                    expectedValue: expectedValue)
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' expected '{expectedValue}' but got '{actualValue}'.",
                    actualValue: actualValue,
                    expectedValue: expectedValue,
                    isRequired: assertion.IsRequired);
        }
    }
}
