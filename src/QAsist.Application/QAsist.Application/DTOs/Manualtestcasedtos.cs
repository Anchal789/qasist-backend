using System.ComponentModel.DataAnnotations;
using QAsist.Domain.Enums;

namespace QAsist.Application.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────────
    // ADD these to your existing DTOs file (or keep in ManualTestCaseDtos.cs)
    // ─────────────────────────────────────────────────────────────────────────────

    // ── Extend your existing TestCaseDto (REPLACE with this) ─────────────────────
    // Key differences from previous version:
    //   - ExpectedResult is string (NOT NULL in DB) — not nullable
    //   - Status values: Draft/Active/Passed/Failed/Skipped/Blocked (matches DB)
    //   - Added UpdatedAt, AssignedTo, new manual builder fields
    public class TestCaseDtoV2      // rename to TestCaseDto in your file
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;   // NOT NULL in DB
        public string Priority { get; set; } = string.Empty;         // "Low"/"Medium"/"High"/"Critical"
        public string Status { get; set; } = string.Empty;           // "Draft"/"Active"/"Passed" etc.
        public bool IsAiGenerated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }                     // NEW
        public Guid? AssignedTo { get; set; }                        // NEW
        // NEW: Manual builder fields
        public string? RequestHeaders { get; set; }
        public string? RequestBody { get; set; }
        public int? ExpectedStatusCode { get; set; }
        public string? ExpectedBodyContains { get; set; }
        public int? ExpectedResponseTimeMs { get; set; }
    }

    // ── Summary DTO for list/table views ─────────────────────────────────────────
    public class TestCaseSummaryDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsAiGenerated { get; set; }
        public Guid? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ── Create manual test case ───────────────────────────────────────────────────
    public class CreateTestCaseDto
    {
        [Required(ErrorMessage = "ProjectId is required.")]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Endpoint is required.")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Endpoint must be 1–500 characters.")]
        public string Endpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Method is required.")]
        [RegularExpression(
            "^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)$",
            ErrorMessage = "Method must be GET, POST, PUT, PATCH, DELETE, HEAD, or OPTIONS.")]
        public string Method { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Title must be 3–500 characters.")]
        public string Title { get; set; } = string.Empty;

        public List<string> Steps { get; set; } = new();

        // NOT NULL in DB — default to empty string if not provided
        public string ExpectedResult { get; set; } = string.Empty;

        public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;

        // Status on create: only Draft or Active make sense
        public TestCaseStatus Status { get; set; } = TestCaseStatus.Draft;

        /// <summary>JSON string: {"Content-Type":"application/json"}</summary>
        public string? RequestHeaders { get; set; }

        public string? RequestBody { get; set; }

        [Range(100, 599, ErrorMessage = "ExpectedStatusCode must be a valid HTTP status code.")]
        public int? ExpectedStatusCode { get; set; }

        public string? ExpectedBodyContains { get; set; }

        [Range(1, 300000, ErrorMessage = "ExpectedResponseTimeMs must be between 1 and 300000 ms.")]
        public int? ExpectedResponseTimeMs { get; set; }

        public Guid? AssignedTo { get; set; }
    }

    // ── Update test case ──────────────────────────────────────────────────────────
    public class UpdateTestCaseDto
    {
        public Guid Id { get; set; }   // set from route in controller

        [Required(ErrorMessage = "Endpoint is required.")]
        [StringLength(500, MinimumLength = 1)]
        public string Endpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Method is required.")]
        [RegularExpression(
            "^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)$",
            ErrorMessage = "Method must be GET, POST, PUT, PATCH, DELETE, HEAD, or OPTIONS.")]
        public string Method { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(500, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        public List<string> Steps { get; set; } = new();

        public string ExpectedResult { get; set; } = string.Empty;  // NOT NULL

        public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;

        public TestCaseStatus Status { get; set; } = TestCaseStatus.Draft;

        public string? RequestHeaders { get; set; }
        public string? RequestBody { get; set; }

        [Range(100, 599)]
        public int? ExpectedStatusCode { get; set; }

        public string? ExpectedBodyContains { get; set; }

        [Range(1, 300000)]
        public int? ExpectedResponseTimeMs { get; set; }

        public Guid? AssignedTo { get; set; }
    }

    // ── Patch status only ─────────────────────────────────────────────────────────
    // DB values: 1=Draft, 2=Active, 3=Passed, 4=Failed, 5=Skipped, 6=Blocked
    public class UpdateTestCaseStatusDto
    {
        [Required]
        public TestCaseStatus Status { get; set; }
    }

    // ── Assign to user ────────────────────────────────────────────────────────────
    public class AssignTestCaseDto
    {
        [Required(ErrorMessage = "AssignedTo user ID is required.")]
        public Guid AssignedTo { get; set; }
    }
}