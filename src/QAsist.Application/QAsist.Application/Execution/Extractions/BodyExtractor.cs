using QAsist.Application.Execution.Assertions;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Execution.Extractions
{
    /// <summary>
    /// T33 — Extracts a value from the response body using JSONPath.
    /// </summary>
    public class BodyExtractor : IExtractor
    {
        public ExtractionSource SupportedSource => ExtractionSource.Body;

        public string? Extract(
            Extraction extraction,
            HttpStepResponse response,
            out string? errorMessage)
        {
            var token = JsonPathHelper.SelectToken(
                response.Body,
                extraction.JsonPath,
                out errorMessage);

            if (errorMessage is not null)
                return null;

            if (token is null)
            {
                errorMessage = $"JSONPath '{extraction.JsonPath}' returned no result.";
                return null;
            }

            return JsonPathHelper.TokenToString(token);
        }
    }
}
