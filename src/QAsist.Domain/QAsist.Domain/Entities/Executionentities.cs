using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T6a — One run of a TestSuite. Parent record for all step results.
    /// Maps to: execution_batches table (NEW table).
    /// Created at the start of execution, updated on completion.
    /// </summary>
    public class ExecutionBatch : BaseEntity
    {
        public Guid TestSuiteId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid EnvironmentId { get; set; }

        public ExecutionStatus Status { get; set; } = ExecutionStatus.Queued;
        public ExecutionTrigger Trigger { get; set; } = ExecutionTrigger.Manual;

        // ── Configuration ──────────────────────────────────────────────────
        public bool FailFast { get; set; } = false;
        public bool ParallelCases { get; set; } = false;

        // ── Summary Counts ─────────────────────────────────────────────────
        public int TotalSteps { get; set; } = 0;
        public int PassedSteps { get; set; } = 0;
        public int FailedSteps { get; set; } = 0;
        public int SkippedSteps { get; set; } = 0;
        public int ErrorSteps { get; set; } = 0;

        // ── Timing ────────────────────────────────────────────────────────
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // ── Computed ──────────────────────────────────────────────────────
        public double PassPercentage =>
            TotalSteps == 0 ? 0
            : Math.Round((double)PassedSteps / TotalSteps * 100, 2);

        public long TotalDurationMs =>
            StartedAt.HasValue && CompletedAt.HasValue
                ? (long)(CompletedAt.Value - StartedAt.Value).TotalMilliseconds
                : 0;

        // ── Navigation ────────────────────────────────────────────────────
        public ICollection<ExecutionResult> Results { get; set; } = new List<ExecutionResult>();
    }

    /// <summary>
    /// T6b — Persisted result of a single TestStep execution.
    /// Maps to: execution_results table (NEW table).
    /// 
    /// Contains full request/response logs and all assertion outcomes.
    /// Written to DB after EACH step completes (not at end of suite).
    /// This ensures partial results are saved even if suite is aborted.
    /// 
    /// request_log and response_log stored as JSONB (sensitive headers redacted).
    /// assertion_results stored as JSONB array.
    /// extracted_variables stored as JSONB key-value object.
    /// </summary>
    public class ExecutionResult : BaseEntity
    {
        public Guid BatchId { get; set; }
        public Guid TestSuiteId { get; set; }
        public Guid TestCaseId { get; set; }   // TestCaseSuite.Id
        public Guid TestStepId { get; set; }

        public StepStatus Status { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }

        public RequestLog? RequestLog { get; set; }
        public ResponseLog? ResponseLog { get; set; }

        public List<AssertionResult> AssertionResults { get; set; } = new();
        public Dictionary<string, string> ExtractedVariables { get; set; } = new();

        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        // ✅ Audit
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // ✅ Navigation
        public ExecutionBatch Batch { get; set; } = null!;
    }
}