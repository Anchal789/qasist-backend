using QAsist.Domain.Enums;

namespace QAsist.Domain.Entities
{
    public class TestCase : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;
        public TestCasePriority Priority { get; set; }
        public TestCaseStatus Status { get; set; }
        public Guid? AssignedTo { get; set; }   // ✅ FIX
        public bool IsAiGenerated { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }

}
