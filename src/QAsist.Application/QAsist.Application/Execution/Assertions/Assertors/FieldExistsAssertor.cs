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
    /// Note: a field with a null VALUE still EXISTS.
    ///   {"data": {"token": null}} → $.data.token EXISTS (Passed)
    ///   {"data": {}}              → $.data.token does NOT EXIST (Failed)
    ///
    /// Example:
    ///   Field: "$.data.token"  Body: {"data":{"token":"abc"}}  → Passed
    ///   Field: "$.data.token"  Body: {"data":{}}               → Failed
    /// </summary>
    public class FieldExistsAssertor : IAssertor
    {
        public AssertionType SupportedType => AssertionType.FieldExists;

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

            // Token not null = field exists
            var passed = token is not null;

            return passed
                ? AssertionResult.Success(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' exists in response body.")
                : AssertionResult.Failure(
                    assertion.Id,
                    SupportedType,
                    $"Field '{assertion.Field}' does not exist in response body.",
                    isRequired: assertion.IsRequired);
        }
    }
}
