using Microsoft.Extensions.Logging;
using QAsist.Application.Execution.Auth;
using QAsist.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QAsist.Application.Execution.Http
{
    /// <summary>
    /// Lives in Infrastructure because it uses System.Net.Http directly.
    ///
    /// Auth injection delegated to AuthInjector (Application layer).
    /// </summary>
    public class RequestBuilder : IRequestBuilder
    {
        private readonly AuthInjector _authInjector;
        private readonly ILogger<RequestBuilder> _logger;

        public RequestBuilder(
            AuthInjector authInjector,
            ILogger<RequestBuilder> logger)
        {
            _authInjector = authInjector;
            _logger = logger;
        }

        public HttpRequestMessage Build(
            string method,
            string resolvedUrl,
            Dictionary<string, string> resolvedHeaders,
            string? resolvedBody,
            AuthConfig? authConfig)
        {
            // Build the base request
            var httpMethod = new System.Net.Http.HttpMethod(method.ToUpperInvariant());
            var request = new HttpRequestMessage(httpMethod, resolvedUrl);

            // Add resolved headers (skip Content-Type — handled with body)
            foreach (var (key, value) in resolvedHeaders)
            {
                if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue; // set with body content below

                request.Headers.TryAddWithoutValidation(key, value);
            }

            // Set request body for methods that support it
            if (!string.IsNullOrEmpty(resolvedBody)
                && MethodSupportsBody(method))
            {
                var contentType = resolvedHeaders
                    .FirstOrDefault(h =>
                        string.Equals(h.Key, "Content-Type",
                            StringComparison.OrdinalIgnoreCase))
                    .Value ?? "application/json";

                request.Content = new StringContent(
                    resolvedBody,
                    Encoding.UTF8,
                    contentType);
            }

            _logger.LogDebug(
                "Built request: {Method} {Url} | Body={HasBody}",
                method, resolvedUrl, resolvedBody is not null);

            return request;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static bool MethodSupportsBody(string method) =>
            method.ToUpperInvariant() is "POST" or "PUT" or "PATCH";

        HttpRequestMessage IRequestBuilder.Build(string method, string resolvedUrl, Dictionary<string, string> resolvedHeaders, string? resolvedBody, AuthConfig? authConfig)
        {
            throw new NotImplementedException();
        }
    }
}
