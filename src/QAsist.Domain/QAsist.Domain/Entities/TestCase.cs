using QAsist.Domain.Enums;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// Maps exactly to public.test_cases table.
    /// steps → JSONB stored as List&lt;string&gt;, serialized by repository.
    /// </summary>
    public class TestCase
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        // ── FIX: was List<string> ─────────────────────────────────────────────
        public List<ExecutableStep> Steps { get; set; } = new();

        public string ExpectedResult { get; set; } = string.Empty;
        public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;
        public TestCaseStatus Status { get; set; } = TestCaseStatus.Draft;
        public bool IsAiGenerated { get; set; }

        // Manual builder fields
        public string? RequestHeaders { get; set; }
        public string? RequestBody { get; set; }
        public int? ExpectedStatusCode { get; set; }
        public string? ExpectedBodyContains { get; set; }
        public int? ExpectedResponseTimeMs { get; set; }

        public Guid? AssignedTo { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // ── Helper: auto-build a single step from flat fields (Step 7 support) ─
        /// <summary>
        /// If Steps is empty, synthesise one step from the flat endpoint/method/body
        /// fields so both "simple mode" and "full step mode" test cases can execute.
        /// </summary>
        public List<ExecutableStep> GetExecutableSteps()
        {
            if (Steps != null && Steps.Any(s => s.IsEnabled))
                return Steps.Where(s => s.IsEnabled).ToList();

            // Simple-mode fallback — build one step from flat fields
            var step = new ExecutableStep
            {
                Name = Title,
                Method = Method,
                Url = Endpoint,
                RequestBody = RequestBody,
                TimeoutMs = 10_000,
                IsEnabled = true
            };

            if (!string.IsNullOrWhiteSpace(RequestHeaders))
            {
                try
                {
                    step.RequestHeaders = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string>>(RequestHeaders)
                        ?? new();
                }
                catch { /* ignore malformed headers */ }
            }

            // Add basic assertions from flat fields
            if (ExpectedStatusCode.HasValue)
                step.Assertions.Add(new StepAssertion
                {
                    Type = "StatusCode",
                    Expected = ExpectedStatusCode.Value.ToString()
                });

            if (!string.IsNullOrWhiteSpace(ExpectedBodyContains))
                step.Assertions.Add(new StepAssertion
                {
                    Type = "BodyContains",
                    Expected = ExpectedBodyContains
                });

            if (ExpectedResponseTimeMs.HasValue)
                step.Assertions.Add(new StepAssertion
                {
                    Type = "ResponseTimeMs",
                    Expected = ExpectedResponseTimeMs.Value.ToString()
                });

            return new List<ExecutableStep> { step };
        }
    }
}

