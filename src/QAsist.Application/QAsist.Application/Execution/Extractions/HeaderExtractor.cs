using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Execution.Extractions
{
    ///   HeaderName: "X-Auth-Token"
    ///   Response Headers: {"x-auth-token": "abc123"}
    ///   Result: "abc123" stored as {{variableName}}
    /// </summary>
    public class HeaderExtractor : IExtractor
    {
        public ExtractionSource SupportedSource => ExtractionSource.Header;

        public string? Extract(
            Extraction extraction,
            HttpStepResponse response,
            out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(extraction.HeaderName))
            {
                errorMessage = "HeaderName is required for Header extraction.";
                return null;
            }

            // Case-insensitive header lookup
            var match = response.Headers
                .FirstOrDefault(h =>
                    string.Equals(h.Key, extraction.HeaderName,
                        StringComparison.OrdinalIgnoreCase));

            if (match.Key is null)
            {
                errorMessage =
                    $"Header '{extraction.HeaderName}' not found in response headers. " +
                    $"Available: [{string.Join(", ", response.Headers.Keys)}]";
                return null;
            }

            return match.Value;
        }
    }
}
