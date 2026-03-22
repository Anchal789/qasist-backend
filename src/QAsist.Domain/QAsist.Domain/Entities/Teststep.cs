using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using HttpMethod = QAsist.Domain.Enums.HttpMethod;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T3 — A single HTTP call within a TestCaseSuite.
    /// Maps to: test_steps table (NEW table).
    /// 
    /// One step = one HTTP request + its assertions + its extractions.
    /// 
    /// URL, headers, and body ALL support {{variable}} placeholders.
    /// These are resolved by VariableResolver before the HTTP call is made.
    /// 
    /// Example:
    ///   Method:  POST
    ///   Url:     {{baseUrl}}/auth/login
    ///   Body:    {"email": "{{testEmail}}", "password": "{{testPassword}}"}
    ///   Extract: $.data.token → {{accessToken}}
    ///   Assert:  status == 200
    /// </summary>
    public class TestStep : BaseEntity
    {
        public Guid TestCaseId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Execution order within the case (0-based).</summary>
        public int OrderIndex { get; set; } = 0;

        // ── HTTP Configuration ────────────────────────────────────────────────
        public HttpMethod Method { get; set; } = HttpMethod.GET;

        /// <summary>
        /// Full URL. Supports {{variable}} placeholders.
        /// Example: "{{baseUrl}}/api/users/{{userId}}"
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Request headers as JSONB key-value.
        /// Values support {{variable}} placeholders.
        /// Example: {"Content-Type": "application/json", "X-Tenant": "{{tenantId}}"}
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = new();

        /// <summary>
        /// Request body as raw string (JSON, XML, form, etc.).
        /// Supports {{variable}} placeholders anywhere in the body.
        /// Example: {"token": "{{access_token}}", "userId": "{{userId}}"}
        /// </summary>
        public string? RequestBody { get; set; }

        // ── Auth Configuration ────────────────────────────────────────────────
        /// <summary>
        /// Step-level auth config. Stored as JSONB.
        /// Overrides case-level and environment-level auth.
        /// Null = inherit from environment.
        /// </summary>
        public AuthConfig? AuthConfig { get; set; }

        // ── Execution Settings ────────────────────────────────────────────────
        /// <summary>Timeout for this step in milliseconds. Default: 10000 (10s).</summary>
        public int TimeoutMs { get; set; } = 10_000;

        /// <summary>
        /// Number of retries on transient HTTP errors (5xx, timeout).
        /// Polly handles the retry with exponential backoff.
        /// Default: 0 (no retry).
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>Disabled steps are skipped during execution.</summary>
        public bool IsEnabled { get; set; } = true;

        public string? Description { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        public TestCaseSuite TestCase { get; set; } = null!;
        public ICollection<Assertion> Assertions { get; set; } = new List<Assertion>();
        public ICollection<Extraction> Extractions { get; set; } = new List<Extraction>();
    }
}