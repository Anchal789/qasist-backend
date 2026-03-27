using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Execution.Http
{
    public interface IRequestBuilder
    {
        /// <summary>
        /// Builds a ready-to-send HttpRequestMessage.
        /// Auth headers injected based on AuthConfig.
        /// </summary>
        HttpRequestMessage Build(
            string method,
            string resolvedUrl,
            Dictionary<string, string> resolvedHeaders,
            string? resolvedBody,
            AuthConfig? authConfig);
    }
    public interface IResponseReader
    {
        Task<HttpStepResponse> ReadAsync(
            HttpResponseMessage httpResponse,
            long durationMs,
            CancellationToken cancellationToken = default);
    }
}
