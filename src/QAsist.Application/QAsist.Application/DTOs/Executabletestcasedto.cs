using System.Text.Json.Serialization;

namespace QAsist.Application.DTOs
{
    /// <summary>
    /// Machine-executable test case with full request/response specification
    /// </summary>
    public class ExecutableTestCaseDto
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "Medium";

        [JsonPropertyName("requestHeaders")]
        public Dictionary<string, string> RequestHeaders { get; set; } = new();

        [JsonPropertyName("requestBody")]
        public object? RequestBody { get; set; }

        [JsonPropertyName("expectedResponse")]
        public ExpectedResponseDto ExpectedResponse { get; set; } = new();

        [JsonPropertyName("requiresAuth")]
        public bool RequiresAuth { get; set; } = true;
    }

    /// <summary>
    /// Expected response validation criteria
    /// </summary>
    public class ExpectedResponseDto
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; } = 200;

        [JsonPropertyName("bodyContains")]
        public List<string> BodyContains { get; set; } = new();

        [JsonPropertyName("bodyEquals")]
        public object? BodyEquals { get; set; }

        [JsonPropertyName("responseTimeMs")]
        public int? ResponseTimeMs { get; set; }

        [JsonPropertyName("requiredFields")]
        public List<string> RequiredFields { get; set; } = new();

        [JsonPropertyName("forbiddenFields")]
        public List<string> ForbiddenFields { get; set; } = new();
    }

    /// <summary>
    /// Request to execute test cases
    /// </summary>
    public class ExecuteTestCasesRequestDto
    {
        public Guid ProjectId { get; set; }
        public string Environment { get; set; } = "Development";
        public List<ExecutableTestCaseDto> TestCases { get; set; } = new();
        public bool SaveResults { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
        public bool FailFast { get; set; } = false;
    }

    /// <summary>
    /// Individual test case execution result
    /// </summary>
    public class TestExecutionResultDto
    {
        public Guid TestExecutionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public TestExecutionStatus Status { get; set; }
        public int ActualStatusCode { get; set; }
        public int ExecutionTimeMs { get; set; }
        public string? FailureReason { get; set; }
        public string? ActualResponse { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DateTime ExecutedAt { get; set; }
    }

    /// <summary>
    /// Overall execution summary
    /// </summary>
    public class TestExecutionSummaryDto
    {
        public Guid ExecutionBatchId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public decimal PassPercentage { get; set; }
        public int TotalExecutionTimeMs { get; set; }
        public List<TestExecutionResultDto> Results { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public enum TestExecutionStatus
    {
        Passed = 1,
        Failed = 2,
        Skipped = 3,
        Error = 4
    }

    /// <summary>
    /// Extended test case generation result with executable format
    /// </summary>
    public class TestCaseGenerationResultDtoV2
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalGenerated { get; set; }

        // Legacy format (for backward compatibility)
        public List<GeneratedTestCaseDto> TestCases { get; set; } = new();

        // NEW: Machine-executable format
        public List<ExecutableTestCaseDto> ExecutableTestCases { get; set; } = new();

        public string Model { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
