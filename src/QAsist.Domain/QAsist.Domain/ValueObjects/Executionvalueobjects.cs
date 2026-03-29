using QAsist.Domain.Enums;

namespace QAsist.Domain.ValueObjects
{
    /// <summary>
    /// T10 — Result of evaluating a single assertion rule.
    /// NOT persisted directly — stored as JSONB array inside ExecutionResult.AssertionResults.
    /// Returned by AssertionEngine.EvaluateAll().
    /// </summary>
    /// 
    
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

    //public class SuiteExecutionResult
    //{
    //    public bool IsSuccess { get; set; }
    //    public int TotalSteps { get; set; }
    //    public int PassedSteps { get; set; }
    //    public int FailedSteps { get; set; }
    //    public int SkippedSteps { get; set; }
    //    public int ErrorSteps { get; set; }
    //    public double PassPercentage { get; set; }
    //    public List<StepExecutionResult> StepResults { get; set; } = new();
    //}

    /// <summary>Result of a single executable step.</summary>
    public class StepExecutionResult
    {
        public Guid TestStepId { get; set; }
        public Guid TestCaseId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public StepStatus Status { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public object? RequestLog { get; set; }
        public object? ResponseLog { get; set; }
        public List<AssertionResult> AssertionResults { get; set; } = new();
        public Dictionary<string, string> ExtractedVariables { get; set; } = new();
        public bool HasFailedRequiredAssertion =>
            AssertionResults.Any(a => !a.Passed && a.IsRequired);

        public bool IsSuccess => Status == StepStatus.Passed;
    }
    

    /// <summary>Result of one assertion within a step.</summary>
   

    /// <summary>Options passed into SuiteExecutor.</summary>
    //public class ExecutionOptions
    //{
    //    public Guid BatchId { get; set; }
    //    public Guid TenantId { get; set; }
    //    public Guid EnvironmentId { get; set; }
    //    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    //    public bool FailFast { get; set; }
    //    public bool ParallelCases { get; set; }
    //}
}