using QAsist.Domain.Enums;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T5 — Extracts a value from the HTTP response and stores it in ExecutionContext.
    /// Maps to: extractions table (NEW table).
    /// 
    /// This is the "chain" that connects steps together.
    /// Step 1 extracts {{token}} → Step 2 uses {{token}} in Authorization header.
    /// 
    /// Extraction runs AFTER assertions, even if assertions fail.
    /// Extraction failures are logged and skipped — they never abort the suite.
    /// 
    /// Examples:
    ///   Body (JSONPath):  source=Body,   jsonPath="$.data.token",  varName="accessToken"
    ///   Header:           source=Header, headerName="X-Auth-Token", varName="authToken"
    ///   Status Code:      source=StatusCode,                        varName="loginStatus"
    /// </summary>
    public class Extraction : BaseEntity
    {
        public Guid TestStepId { get; set; }

        /// <summary>
        /// Name of the variable to create in ExecutionContext.
        /// Must match pattern: [a-zA-Z][a-zA-Z0-9_]*
        /// Used as {{variableName}} in subsequent steps.
        /// Example: "accessToken", "userId", "orderId"
        /// </summary>
        public string VariableName { get; set; } = string.Empty;

        public ExtractionSource Source { get; set; } = ExtractionSource.Body;

        // ── Body Extraction (Source = Body) ───────────────────────────────────
        /// <summary>
        /// JSONPath expression to extract value from response body.
        /// Required when Source = Body.
        /// Examples: "$.data.id", "$.users[0].email", "$.access_token"
        /// When path returns an array, the FIRST element is stored.
        /// </summary>
        public string? JsonPath { get; set; }

        // ── Header Extraction (Source = Header) ───────────────────────────────
        /// <summary>
        /// Response header name to extract.
        /// Required when Source = Header.
        /// Header name matching is case-insensitive.
        /// Example: "X-Auth-Token", "Location", "ETag"
        /// </summary>
        public string? HeaderName { get; set; }

        // ── Fallback ──────────────────────────────────────────────────────────
        /// <summary>
        /// Value stored in context when extraction fails (path not found, header missing).
        /// If null and extraction fails: logs warning, stores empty string.
        /// Example: "anonymous", "0", ""
        /// </summary>
        public string? DefaultValue { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        public TestStep Step { get; set; } = null!;
    }
}