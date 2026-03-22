using QAsist.Domain.Enums;

namespace QAsist.Domain.ValueObjects
{
    /// <summary>
    /// T10 — Result of evaluating a single assertion rule.
    /// NOT persisted directly — stored as JSONB array inside ExecutionResult.AssertionResults.
    /// Returned by AssertionEngine.EvaluateAll().
    /// </summary>
    public sealed class AssertionResult
    {
        public Guid AssertionId { get; init; }
        public AssertionType AssertionType { get; init; }
        public bool Passed { get; init; }
        public string? ActualValue { get; init; }
        public string? ExpectedValue { get; init; }

        /// <summary>Human-readable message — shown in execution result UI.</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// If true, failure marks the step as Failed.
        /// If false, failure is a warning only — step can still pass.
        /// </summary>
        public bool IsRequired { get; init; } = true;

        // ── Factory Methods ───────────────────────────────────────────────────

        public static AssertionResult Success(
            Guid assertionId,
            AssertionType type,
            string message,
            string? actualValue = null,
            string? expectedValue = null) => new()
            {
                AssertionId = assertionId,
                AssertionType = type,
                Passed = true,
                Message = message,
                ActualValue = actualValue,
                ExpectedValue = expectedValue
            };

        public static AssertionResult Failure(
            Guid assertionId,
            AssertionType type,
            string message,
            string? actualValue = null,
            string? expectedValue = null,
            bool isRequired = true) => new()
            {
                AssertionId = assertionId,
                AssertionType = type,
                Passed = false,
                Message = message,
                ActualValue = actualValue,
                ExpectedValue = expectedValue,
                IsRequired = isRequired
            };

        public static AssertionResult Error(
            Guid assertionId,
            AssertionType type,
            string errorMessage) => new()
            {
                AssertionId = assertionId,
                AssertionType = type,
                Passed = false,
                Message = $"Assertion engine error: {errorMessage}",
                IsRequired = true
            };
    }

    /// <summary>
    /// Result of executing a single TestStep.
    /// NOT persisted directly — used in-memory during suite execution,
    /// then mapped to ExecutionResult entity for DB storage.
    /// </summary>
    public sealed class StepExecutionResult
    {
        public Guid TestStepId { get; init; }
        public StepStatus Status { get; init; }
        public long DurationMs { get; init; }

        // ── Logs (stored as JSONB) ────────────────────────────────────────────
        public RequestLog? RequestLog { get; init; }
        public ResponseLog? ResponseLog { get; init; }

        // ── Assertion Results ─────────────────────────────────────────────────
        public IReadOnlyList<AssertionResult> AssertionResults { get; init; }
            = Array.Empty<AssertionResult>();

        // ── Extracted Variables ───────────────────────────────────────────────
        /// <summary>Variables extracted from this step's response.</summary>
        public IReadOnlyDictionary<string, string> ExtractedVariables { get; init; }
            = new Dictionary<string, string>();

        // ── Error ─────────────────────────────────────────────────────────────
        public string? ErrorMessage { get; init; }

        // ── Helpers ───────────────────────────────────────────────────────────
        public bool IsSuccess => Status == StepStatus.Passed;

        public bool HasFailedRequiredAssertion =>
            AssertionResults.Any(a => !a.Passed && a.IsRequired);
    }

    /// <summary>
    /// Structured log of the outgoing HTTP request.
    /// Auth headers are REDACTED before storage.
    /// </summary>
    public sealed class RequestLog
    {
        public string Method { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;

        /// <summary>Headers with Authorization values replaced by "[REDACTED]".</summary>
        public Dictionary<string, string> Headers { get; init; } = new();

        public string? Body { get; init; }
        public DateTime SentAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Structured log of the incoming HTTP response.
    /// Body is truncated at 100KB to prevent DB bloat.
    /// </summary>
    public sealed class ResponseLog
    {
        public int StatusCode { get; init; }
        public Dictionary<string, string> Headers { get; init; } = new();

        /// <summary>Response body — max 100KB. Truncated with "[TRUNCATED]" suffix if larger.</summary>
        public string? Body { get; init; }

        public long DurationMs { get; init; }
        public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;

        /// <summary>Max body size stored in logs (100KB).</summary>
        public const int MaxBodyBytes = 102_400;
    }
}