namespace QAsist.Domain.Entities
{
    /// <summary>
    /// Test environment configuration (Development, Staging, Production)
    /// </summary>
    public class Environment : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> GlobalHeaders { get; set; } = new();
        public bool IsProduction { get; set; }
        public bool AllowExecution { get; set; } = true;

        // Navigation
        public Project Project { get; set; } = null!;
    }
}
