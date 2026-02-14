using System.ComponentModel.DataAnnotations;

namespace QAsist.Application.DTOs
{
    /// <summary>
    /// Request DTO for AI test case generation
    /// </summary>
    public class GenerateTestCasesRequestDto
    {
        /// <summary>
        /// Project ID - REQUIRED. All test cases belong to a project.
        /// </summary>
        [Required(ErrorMessage = "ProjectId is required. Test cases must belong to a project.")]
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Optional: Directly provide OpenAPI JSON spec
        /// </summary>
        public string? OpenApiJson { get; set; }

        /// <summary>
        /// If true, backend fetches swagger.json from running API
        /// </summary>
        public bool UseDefaultSpec { get; set; } = true;

        /// <summary>
        /// Phase 2: Save generated test cases to database
        /// </summary>
        public bool SaveToDatabase { get; set; } = false;
    }

    /// <summary>
    /// Single generated test case from AI
    /// </summary>
    public class GeneratedTestCaseDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of AI test case generation
    /// </summary>
    public class TestCaseGenerationResultDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalGenerated { get; set; }
        // Legacy format (for backward compatibility and Excel export)
        public List<GeneratedTestCaseDto> TestCases { get; set; } = new();

        // NEW: Machine-executable format for test execution
        public List<ExecutableTestCaseDto> ExecutableTestCases { get; set; } = new();

        public string Model { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request DTO for Excel export
    /// </summary>
    public class ExportTestCasesRequestDto
    {
        [Required(ErrorMessage = "ProjectId is required")]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "TestCases are required")]
        [MinLength(1, ErrorMessage = "At least one test case is required")]
        public List<GeneratedTestCaseDto> TestCases { get; set; } = new();
    }

    /// <summary>
    /// Test case DTO (for future persistence)
    /// </summary>
    public class TestCaseDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsAiGenerated { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
