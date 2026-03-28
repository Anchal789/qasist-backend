namespace QAsist.Domain.Entities
{
    public class TestSuiteTestCase : BaseEntity
    {
        /// <summary>FK → test_suites.id</summary>
        public Guid TestSuiteId { get; set; }

        /// <summary>FK → test_cases.id (your existing flat test cases)</summary>
        public Guid TestCaseId { get; set; }

        /// <summary>Execution order (0-based). Lower = runs first.</summary>
        public int Order { get; set; } = 0;

        /// <summary>Whether this mapping is active (can disable without removing).</summary>
        public bool IsEnabled { get; set; } = true;

        // ── Navigation ────────────────────────────────────────────────────────
        public TestSuite Suite { get; set; } = null!;
        public TestCase TestCase { get; set; } = null!;
    }
}
