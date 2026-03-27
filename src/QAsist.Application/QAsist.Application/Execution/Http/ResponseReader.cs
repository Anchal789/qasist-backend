using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext.IAssertions;

namespace QAsist.Application.Execution.Http
{
    /// <summary>
    /// Week 6 — Reads HttpResponseMessage into HttpStepResponse.
    ///
    /// Body truncation: bodies larger than 100KB are truncated with
    /// "[TRUNCATED - {size}KB total]" suffix to prevent DB bloat.
    /// </summary>
    public class ResponseReader : IResponseReader
    {
        private readonly ILogger<ResponseReader> _logger;
        private const int MaxBodyBytes = 102_400; // 100KB

        public ResponseReader(ILogger<ResponseReader> logger)
        {
            _logger = logger;
        }

        public async Task<HttpStepResponse> ReadAsync(
            HttpResponseMessage httpResponse,
            long durationMs,
            CancellationToken cancellationToken = default)
        {
            // Read headers (flatten multi-value headers to comma-separated)
            var headers = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var header in httpResponse.Headers)
                headers[header.Key] = string.Join(", ", header.Value);

            foreach (var header in httpResponse.Content.Headers)
                headers[header.Key] = string.Join(", ", header.Value);

            // Read body with size limit
            string body;
            try
            {
                var bytes = await httpResponse.Content
                    .ReadAsByteArrayAsync(cancellationToken);

                if (bytes.Length <= MaxBodyBytes)
                {
                    body = System.Text.Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    var truncated = System.Text.Encoding.UTF8.GetString(
                        bytes, 0, MaxBodyBytes);
                    var totalKb = bytes.Length / 1024;
                    body = truncated +
                           $"\n[TRUNCATED — {totalKb}KB total, showing first 100KB]";

                    _logger.LogWarning(
                        "Response body truncated. Total size: {TotalKb}KB", totalKb);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read response body. Using empty string.");
                body = string.Empty;
            }

            return new HttpStepResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                Body = body,
                Headers = headers,
                DurationMs = durationMs
            };
        }
    }
}
