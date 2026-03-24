using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Execution.Extractions
{
    /// Example:
    ///   Source: StatusCode
    ///   VariableName: "loginStatus"
    ///   Response: 201 Created
    ///   Result: ctx["loginStatus"] = "201"
    /// </summary>
    public class StatusCodeExtractor : IExtractor
    {
        public ExtractionSource SupportedSource => ExtractionSource.StatusCode;

        public string? Extract(
            Extraction extraction,
            HttpStepResponse response,
            out string? errorMessage)
        {
            errorMessage = null;
            // Always succeeds — status code is always present
            return response.StatusCode.ToString();
        }
    }
}
