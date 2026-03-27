using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Application.Execution.Auth
{
    /// SECURITY: Auth header values are resolved BEFORE injection.
    ///           Bearer {{token}} → Bearer eyJhbG...
    ///           The actual token value is NEVER logged.
    /// </summary>
    public class AuthInjector
    {
        private readonly IVariableResolver _resolver;
        private readonly ILogger<AuthInjector> _logger;

        public AuthInjector(
            IVariableResolver resolver,
            ILogger<AuthInjector> logger)
        {
            _resolver = resolver;
            _logger = logger;
        }

        /// <summary>
        /// Resolves auth config variables and injects headers into request.
        /// </summary>
        public void Inject(
            HttpRequestMessage request,
            AuthConfig? authConfig,
            IExecutionContext context)
        {
            if (authConfig is null || authConfig.AuthType == AuthType.None)
            {
                _logger.LogDebug("No auth config — skipping auth injection.");
                return;
            }

            switch (authConfig.AuthType)
            {
                case AuthType.Bearer:
                    InjectBearer(request, authConfig, context);
                    break;

                case AuthType.ApiKey:
                    InjectApiKey(request, authConfig, context);
                    break;

                case AuthType.Custom:
                    InjectCustom(request, authConfig, context);
                    break;

                default:
                    _logger.LogWarning("Unknown AuthType: {Type}", authConfig.AuthType);
                    break;
            }
        }

        // ── Week 7: Resolve auth priority chain ──────────────────────────────
        /// <summary>
        /// Returns the highest-priority non-None auth config.
        /// Step > Case > Environment. First non-null with AuthType != None wins.
        /// </summary>
        public static AuthConfig? ResolveAuthConfig(
            AuthConfig? stepAuth,
            AuthConfig? environmentAuth)
        {
            if (stepAuth is not null && stepAuth.AuthType != AuthType.None)
                return stepAuth;

            if (environmentAuth is not null && environmentAuth.AuthType != AuthType.None)
                return environmentAuth;

            return null;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void InjectBearer(
            HttpRequestMessage request,
            AuthConfig authConfig,
            IExecutionContext context)
        {
            var token = _resolver.Resolve(authConfig.Token, context);
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Bearer token resolved to empty. Auth header not set.");
                return;
            }

            request.Headers.TryAddWithoutValidation(
                "Authorization", $"Bearer {token}");

            _logger.LogDebug("Bearer auth injected. Token: [REDACTED]");
        }

        private void InjectApiKey(
            HttpRequestMessage request,
            AuthConfig authConfig,
            IExecutionContext context)
        {
            var headerName = _resolver.Resolve(authConfig.HeaderName, context);
            var headerValue = _resolver.Resolve(authConfig.HeaderValue, context);

            if (string.IsNullOrWhiteSpace(headerName))
            {
                _logger.LogWarning("ApiKey HeaderName resolved to empty. Skipping.");
                return;
            }

            request.Headers.TryAddWithoutValidation(headerName, headerValue);
            _logger.LogDebug("ApiKey auth injected. Header: {Name}", headerName);
        }

        private void InjectCustom(
            HttpRequestMessage request,
            AuthConfig authConfig,
            IExecutionContext context)
        {
            foreach (var (key, value) in authConfig.CustomHeaders)
            {
                var resolvedKey = _resolver.Resolve(key, context);
                var resolvedValue = _resolver.Resolve(value, context);

                if (string.IsNullOrWhiteSpace(resolvedKey))
                    continue;

                request.Headers.TryAddWithoutValidation(resolvedKey, resolvedValue);
            }
            _logger.LogDebug(
                "Custom auth injected. Headers: {Count}", authConfig.CustomHeaders.Count);
        }
    }
}
