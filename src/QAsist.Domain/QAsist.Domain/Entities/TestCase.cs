using QAsist.Domain.Enums;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// Maps exactly to public.test_cases table.
    /// steps → JSONB stored as List&lt;string&gt;, serialized by repository.
    /// </summary>
    public class TestCase
    {
        // ── Primary ───────────────────────────────────────────────────────────
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }

        // ── HTTP ─────────────────────────────────────────────────────────────
        public string Endpoint { get; set; } = string.Empty;       // VARCHAR(500) NOT NULL
        public string Method { get; set; } = string.Empty;         // VARCHAR(10)  NOT NULL

        // ── Test Content ──────────────────────────────────────────────────────
        public string Title { get; set; } = string.Empty;          // VARCHAR(500) NOT NULL
        public List<string> Steps { get; set; } = new();           // JSONB NOT NULL DEFAULT '[]'
        public string ExpectedResult { get; set; } = string.Empty; // TEXT NOT NULL

        // ── Classification ───────────────────────────────────────────────────
        public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;  // INT NOT NULL
        public TestCaseStatus Status { get; set; } = TestCaseStatus.Draft;         // INT NOT NULL DEFAULT 1

        // ── Manual Builder fields (new columns added via migration) ───────────
        public string? RequestHeaders { get; set; }         // JSONB  — nullable new column
        public string? RequestBody { get; set; }            // TEXT   — nullable new column
        public int? ExpectedStatusCode { get; set; }        // INT    — nullable new column
        public string? ExpectedBodyContains { get; set; }   // TEXT   — nullable new column
        public int? ExpectedResponseTimeMs { get; set; }    // INT    — nullable new column

        // ── Ownership ────────────────────────────────────────────────────────
        public Guid? AssignedTo { get; set; }               // UUID FK → users.id
        public bool IsAiGenerated { get; set; } = false;    // BOOLEAN NOT NULL DEFAULT false

        // ── Audit ─────────────────────────────────────────────────────────────
        public Guid CreatedBy { get; set; }                 // UUID NOT NULL FK → users.id
        public DateTime CreatedAt { get; set; }             // TIMESTAMP NOT NULL DEFAULT now()
        public Guid? UpdatedBy { get; set; }                // UUID FK → users.id
        public DateTime? UpdatedAt { get; set; }            // TIMESTAMP

        // ── Soft Delete ───────────────────────────────────────────────────────
        public bool IsDeleted { get; set; } = false;        // BOOLEAN NOT NULL DEFAULT false
        public DateTime? DeletedAt { get; set; }            // TIMESTAMP  (your table has this)
        public Guid? DeletedBy { get; set; }                // UUID FK → users.id (your table has this)
    }
}