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
    /// Commonly used to assert no error field in successful responses:
    ///   Assert "$.error" does NOT exist → confirms clean success response
    ///
    /// Example:
    ///   Field: "$.error"  Body: {"data":{"id":"1"}}   → Passed (no error field)
    ///   Field: "$.error"  Body: {"error":"not found"}  → Failed (error field present)
    /// </summary>
    public class FieldNotExistsAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.FieldNotExists;

        public AssertionResult Evaluate(
            Domain.Entities.Assertion assertion,
            HttpStepResponse response,
            long durationMs)
        {
            var token = JsonPathHelper.SelectToken(
                response.Body,
                assertion.Field,
                out var errorMessage);

            // JSONPath evaluation error
            if (errorMessage is not null)
            {
                return AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    errorMessage,
                    isRequired: assertion.IsRequired);
            }

            // Token null = field does NOT exist = Passed
            var passed = token is null;

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' correctly does not exist in response body.")
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' exists in response body but was expected to be absent. " +
                    $"Value: '{JsonPathHelper.TokenToString(token)}'.",
                    actualValue: JsonPathHelper.TokenToString(token),
                    isRequired: assertion.IsRequired);
        }
    }
}
