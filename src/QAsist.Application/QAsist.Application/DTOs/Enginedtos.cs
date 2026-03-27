using System.ComponentModel.DataAnnotations;

namespace QAsist.Application.DTOs
{
    public class Enginedtos
    {
        public class TestSuiteDto
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public Dictionary<string, string> Variables { get; set; } = new();
            public bool ParallelCases { get; set; }
            public bool IsActive { get; set; }
            public int TotalCases { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class TestSuiteDetailDto : TestSuiteDto
        {
            public List<TestCaseSuiteDto> TestCases { get; set; } = new();
        }

        public class TestCaseSuiteDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int OrderIndex { get; set; }
            public bool IsEnabled { get; set; }
            public List<string> Tags { get; set; } = new();
            public List<TestStepDto> Steps { get; set; } = new();
        }

        public class TestStepDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int OrderIndex { get; set; }
            public string Method { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public Dictionary<string, string> RequestHeaders { get; set; } = new();
            public string? RequestBody { get; set; }
            public int TimeoutMs { get; set; }
            public int RetryCount { get; set; }
            public bool IsEnabled { get; set; }
            public List<AssertionDto> Assertions { get; set; } = new();
            public List<ExtractionDto> Extractions { get; set; } = new();
        }

        public class AssertionDto
        {
            public Guid Id { get; set; }
            public string AssertionType { get; set; } = string.Empty;
            public string? Field { get; set; }
            public string? Operator { get; set; }
            public string? ExpectedValue { get; set; }
            public int OrderIndex { get; set; }
            public bool IsRequired { get; set; }
        }

        public class ExtractionDto
        {
            public Guid Id { get; set; }
            public string VariableName { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string? JsonPath { get; set; }
            public string? HeaderName { get; set; }
            public string? DefaultValue { get; set; }
        }

        // ── Create / Update DTOs ──────────────────────────────────────────────────

        public class CreateTestSuiteDto
        {
            [Required]
            public Guid ProjectId { get; set; }

            [Required]
            [StringLength(200, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }
            public Dictionary<string, string> Variables { get; set; } = new();
            public bool ParallelCases { get; set; } = false;
        }

        public class UpdateTestSuiteDto
        {
            public Guid Id { get; set; }  // set from route

            [Required]
            [StringLength(200, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }
            public Dictionary<string, string> Variables { get; set; } = new();
            public bool ParallelCases { get; set; }
            public bool IsActive { get; set; } = true;
        }

        // ── Execution DTOs ────────────────────────────────────────────────────────

        public class ExecuteSuiteRequestDto
        {
            [Required]
            public Guid EnvironmentId { get; set; }

            public bool FailFast { get; set; } = false;
            public bool ParallelCases { get; set; } = false;
        }

        public class ExecuteSuiteResponseDto
        {
            public Guid BatchId { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime StartedAt { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public class ExecutionBatchDto
        {
            public Guid BatchId { get; set; }
            public Guid SuiteId { get; set; }
            public Guid ProjectId { get; set; }
            public string Status { get; set; } = string.Empty;
            public int TotalSteps { get; set; }
            public int PassedSteps { get; set; }
            public int FailedSteps { get; set; }
            public int SkippedSteps { get; set; }
            public int ErrorSteps { get; set; }
            public double PassPercentage { get; set; }
            public long TotalDurationMs { get; set; }
            public DateTime? StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        public class StepResultDto
        {
            public Guid Id { get; set; }
            public Guid BatchId { get; set; }
            public Guid TestStepId { get; set; }
            public string Status { get; set; } = string.Empty;
            public long DurationMs { get; set; }
            public string? ErrorMessage { get; set; }
            public object? RequestLog { get; set; }
            public object? ResponseLog { get; set; }
            public List<AssertionResultDto> AssertionResults { get; set; } = new();
            public Dictionary<string, string> ExtractedVariables { get; set; } = new();
            public DateTime ExecutedAt { get; set; }
        }

        public class AssertionResultDto
        {
            public string AssertionType { get; set; } = string.Empty;
            public bool Passed { get; set; }
            public string? ActualValue { get; set; }
            public string? ExpectedValue { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
