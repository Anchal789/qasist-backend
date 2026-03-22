using QAsist.Domain.Enums;

namespace QAsist.Domain.ValueObjects
{
    /// <summary>
    /// T9 — Immutable authentication configuration.
    /// Stored as JSONB in test_steps.auth_config and environments.auth_config columns.
    /// 
    /// Priority chain (highest wins):
    ///   TestStep.AuthConfig > TestCase.AuthConfig > Environment.AuthConfig
    /// 
    /// Examples:
    ///   Bearer  → Authorization: Bearer {{access_token}}
    ///   ApiKey  → X-Api-Key: {{api_key}}
    ///   Custom  → Any header name = any value
    /// </summary>
    public sealed class AuthConfig
    {
        public AuthType AuthType { get; init; } = AuthType.None;

        // ── Bearer Token ──────────────────────────────────────────────────────
        /// <summary>
        /// Bearer token value. Supports {{variable}} placeholders.
        /// e.g. "{{access_token}}"
        /// </summary>
        public string? Token { get; init; }

        // ── API Key ───────────────────────────────────────────────────────────
        /// <summary>
        /// Header name for API key auth.
        /// e.g. "X-Api-Key" or "Authorization"
        /// </summary>
        public string? HeaderName { get; init; }

        /// <summary>
        /// Header value for API key auth. Supports {{variable}} placeholders.
        /// e.g. "{{api_key}}"
        /// </summary>
        public string? HeaderValue { get; init; }

        // ── Custom Headers ────────────────────────────────────────────────────
        /// <summary>
        /// Multiple custom headers injected into the request.
        /// Each value supports {{variable}} placeholders.
        /// Used when AuthType = Custom.
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; init; } = new();

        // ── Factory Methods ───────────────────────────────────────────────────

        public static AuthConfig None() => new() { AuthType = AuthType.None };

        public static AuthConfig Bearer(string token) => new()
        {
            AuthType = AuthType.Bearer,
            Token = token
        };

        public static AuthConfig ApiKey(string headerName, string headerValue) => new()
        {
            AuthType = AuthType.ApiKey,
            HeaderName = headerName,
            HeaderValue = headerValue
        };

        public static AuthConfig Custom(Dictionary<string, string> headers) => new()
        {
            AuthType = AuthType.Custom,
            CustomHeaders = headers
        };

        // ── Validation ────────────────────────────────────────────────────────

        public bool IsValid() => AuthType switch
        {
            AuthType.None => true,
            AuthType.Bearer => !string.IsNullOrWhiteSpace(Token),
            AuthType.ApiKey => !string.IsNullOrWhiteSpace(HeaderName)
                             && !string.IsNullOrWhiteSpace(HeaderValue),
            AuthType.Custom => CustomHeaders.Count > 0,
            _ => false
        };
    }
}