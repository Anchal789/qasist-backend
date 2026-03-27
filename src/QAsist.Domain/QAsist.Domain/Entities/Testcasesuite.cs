namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T2 — An ordered test case within a TestSuite.
    /// Maps to: test_cases_suite table (NEW table — separate from existing test_cases).
    /// 
    /// NAMING NOTE:
    /// Your existing "test_cases" table stores AI-generated and manual test cases
    /// (flat structure). This entity is the ENGINE's richer test case — it has
    /// ordered steps, assertions, and extractions per step.
    /// 
    /// Naming convention used here: TestCaseSuite to avoid conflict with
    /// your existing TestCase entity. You can rename to SuiteTestCase if preferred.
    /// 
    /// Analogy: A "folder" in Postman — contains ordered requests.
    /// </summary>
    public class TestCaseSuite : BaseEntity
    {
        public Guid TestSuiteId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Execution order within the suite (0-based). Lower = runs first.</summary>
        public int OrderIndex { get; set; } = 0;

        /// <summary>Disabled cases are skipped during execution.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Optional tags for filtering e.g. ["smoke", "auth", "regression"]</summary>
        public List<string> Tags { get; set; } = new();

        // ── Navigation ────────────────────────────────────────────────────────
        public TestSuite Suite { get; set; } = null!;
        public ICollection<TestStep> Steps { get; set; } = new List<TestStep>();
    }
}
