namespace QAsist.Application.DTOs
{
    /// <summary>
    /// Project environment configuration
    /// </summary>
    public class EnvironmentDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> GlobalHeaders { get; set; } = new();
        public bool IsProduction { get; set; }
        public bool AllowExecution { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Create environment request
    /// </summary>
    public class CreateEnvironmentDto
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> GlobalHeaders { get; set; } = new();
        public bool IsProduction { get; set; }
    }

    /// <summary>
    /// Update environment request
    /// </summary>
    public class UpdateEnvironmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> GlobalHeaders { get; set; } = new();
        public bool IsProduction { get; set; }
        public bool AllowExecution { get; set; } = true;
    }
}
