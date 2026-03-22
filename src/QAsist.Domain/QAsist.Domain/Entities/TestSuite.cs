namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T1 — Root aggregate for a collection of test cases.
    /// Maps to: test_suites table.
    /// 
    /// Hierarchy: TestSuite → TestCase → TestStep → Assertion / Extraction
    /// 
    /// Analogy: A "collection" in Postman — groups related test cases together.
    /// Suite-level variables are the default values seeded into ExecutionContext
    /// before the run starts.
    /// </summary>
    public class TestSuite : BaseEntity
    {
        public Guid ProjectId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Default variables for this suite (key=value pairs).
        /// Stored as JSONB. Seeded into ExecutionContext before execution starts.
        /// Can be overridden by environment variables or runtime extractions.
        /// Example: {"baseUrl": "https://api.dev.io", "timeout": "5000"}
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new();

        /// <summary>
        /// When true, test cases run in parallel via Task.WhenAll.
        /// When false (default), cases run sequentially in order.
        /// WARNING: parallel=true means variables extracted in Case 1
        /// are NOT available in Case 2 (race condition). Use sequential
        /// when cases depend on each other.
        /// </summary>
        public bool ParallelCases { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // ── Navigation ────────────────────────────────────────────────────────
        public ICollection<TestCaseSuite> TestCases { get; set; } = new List<TestCaseSuite>();
    }
}