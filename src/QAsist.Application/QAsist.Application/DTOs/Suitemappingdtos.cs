using System.ComponentModel.DataAnnotations;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Application.DTOs
{
    public class SuiteMappingDtos
    {
        // ── REQUEST DTOs ─────────────────────────────────────────────

        /// <summary>Attach one or more test cases to a suite.</summary>
        public class AddTestCasesToSuiteDto
        {
            [Required(ErrorMessage = "At least one test case is required.")]
            [MinLength(1, ErrorMessage = "At least one test case is required.")]
            public List<TestCaseSuiteMappingDto> TestCases { get; set; } = new();
        }

        public class TestCaseSuiteMappingDto
        {
            [Required]
            public Guid TestCaseId { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Order must be 0 or greater.")]
            public int Order { get; set; } = 0;

            public bool IsEnabled { get; set; } = true;
        }

        /// <summary>Reorder test cases within a suite.</summary>
        public class ReorderSuiteTestCasesDto
        {
            [Required]
            public List<TestCaseOrderDto> Order { get; set; } = new();
        }

        public class TestCaseOrderDto
        {
            [Required]
            public Guid MappingId { get; set; }

            [Range(0, int.MaxValue)]
            public int Order { get; set; }
        }

        /// <summary>Execute a test suite.</summary>
        public class ExecuteSuiteDto
        {
            [Required(ErrorMessage = "EnvironmentId is required.")]
            public Guid EnvironmentId { get; set; }

            public bool FailFast { get; set; } = false;
            public bool ParallelCases { get; set; } = false;
        }

        // ── RESPONSE DTOs ────────────────────────────────────────────

        /// <summary>Mapping response for suite test cases.</summary>
        public class SuiteTestCaseMappingDto
        {
            public Guid MappingId { get; set; }
            public Guid TestSuiteId { get; set; }
            public Guid TestCaseId { get; set; }
            public string TestCaseTitle { get; set; } = string.Empty;
            public string Endpoint { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public int Order { get; set; }
            public bool IsEnabled { get; set; }
            public string Priority { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? HttpMethod { get; set; }
        }

        public class AddTestCasesResultDto
        {
            public int AddedCount { get; set; }
            public IEnumerable<Guid> MappingIds { get; set; } = new List<Guid>();
            public int SkippedCount { get; set; }
        }

        /// <summary>Result of executing one test case.</summary>
        //public class StepResultDto
        //{
        //    public Guid Id { get; set; }
        //    public Guid BatchId { get; set; }
        //    public Guid TestCaseId { get; set; }
        //    public string TestCaseTitle { get; set; } = string.Empty;
        //    public string Endpoint { get; set; } = string.Empty;
        //    public string Method { get; set; } = string.Empty;
        //    public int Order { get; set; }

        //    public string Status { get; set; } = string.Empty; // Pass / Fail / Error
        //    public int? StatusCode { get; set; }
        //    public int? ResponseTimeMs { get; set; }
        //    public string? ErrorMessage { get; set; }
        //    public string? FailureReason { get; set; }
        //    public string? HttpMethod { get; set; }
        //    public string? FullUrl { get; set; }
        //    public string? RequestBody { get; set; }
        //    public string? ResponseBody { get; set; }

        //    public DateTime StartedAt { get; set; }
        //    public long DurationMs { get; set; }
        //    public DateTime? CompletedAt { get; set; }

            
        //}

        /// <summary>Summary of a full suite execution.</summary>
        public class SuiteExecutionBatchDto
        {
            public Guid BatchId { get; set; }
            public Guid SuiteId { get; set; }
            public Guid ProjectId { get; set; }

            public string Status { get; set; } = string.Empty; // Queued/Running/Completed
            public string SuiteType { get; set; } = string.Empty;

            public int TotalSteps { get; set; }
            public int PassedSteps { get; set; }
            public int FailedSteps { get; set; }
            public int SkippedSteps { get; set; }
            public int ErrorSteps { get; set; }

            public double PassPercentage { get; set; }
            public long TotalDurationMs { get; set; }

            public DateTime? StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }

            public List<ExecutionStepResultDto> StepResults { get; set; } = new();
        }
    }
}