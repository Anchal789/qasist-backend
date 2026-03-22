using QAsist.Domain.Enums;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T4 — A validation rule evaluated against a step's HTTP response.
    /// Maps to: assertions table (NEW table).
    /// 
    /// Uses Strategy Pattern — AssertionType determines which IAssertor runs.
    /// 
    /// Examples:
    ///   StatusCodeEquals    → field: null,       expected: "200"
    ///   ResponseTimeLessThan→ field: null,       expected: "500"  (ms)
    ///   BodyContains        → field: null,       expected: "success"
    ///   FieldEquals         → field: "$.data.id",expected: "{{userId}}"
    ///   FieldExists         → field: "$.data.token"
    ///   FieldNotExists      → field: "$.error"
    ///   FieldMatchesRegex   → field: "$.email",  expected: "^[^@]+@[^@]+$"
    /// </summary>
    public class Assertion : BaseEntity
    {
        public Guid TestStepId { get; set; }

        public AssertionType AssertionType { get; set; }

        /// <summary>
        /// JSONPath expression for field-based assertions (FieldEquals, FieldExists, etc.)
        /// or response header name for header assertions.
        /// Null for StatusCodeEquals, ResponseTimeLessThan, BodyContains.
        /// Example: "$.data.id", "$.users[0].email", "X-Request-Id"
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Comparison operator for field assertions.
        /// Values: "equals", "contains", "startsWith", "endsWith", "greaterThan", "lessThan"
        /// Null for FieldExists / FieldNotExists (no comparison needed).
        /// </summary>
        public string? Operator { get; set; }

        /// <summary>
        /// Expected value to compare against.
        /// Supports {{variable}} placeholders — resolved before assertion runs.
        /// Example: "200", "{{userId}}", "success"
        /// </summary>
        public string? ExpectedValue { get; set; }

        /// <summary>Evaluation order within the step (lower = evaluated first).</summary>
        public int OrderIndex { get; set; } = 0;

        /// <summary>
        /// When true (default): failure marks the step as Failed.
        /// When false: failure is a WARNING only — step can still pass.
        /// Useful for soft validations (e.g. optional fields).
        /// </summary>
        public bool IsRequired { get; set; } = true;

        // ── Navigation ────────────────────────────────────────────────────────
        public TestStep Step { get; set; } = null!;
    }
}