using System.ComponentModel.DataAnnotations;

namespace QAsist.Application.DTOs.Monitoring
{
    public class CreateMonitoredEndpointDto
    {
        [Required] public Guid ProjectId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Url(ErrorMessage = "URL must be a valid HTTP/HTTPS URL.")]
        public string Url { get; set; } = string.Empty;

        [RegularExpression("^(GET|POST|PUT|DELETE|HEAD)$")]
        public string Method { get; set; } = "GET";

        public Dictionary<string, string> RequestHeaders { get; set; } = new();
        public string? RequestBody { get; set; }
        public int ExpectedStatusCode { get; set; } = 200;
        public string? ContentContains { get; set; }
        public int TimeoutMs { get; set; } = 10_000;

        // Cron: */1=1min */5=5min */15=15min 0 * * * *=hourly
        [Required]
        public string FrequencyCron { get; set; } = "*/5 * * * *";
        public bool IsActive { get; set; } = true;
    }

    public class UpdateMonitoredEndpointDto : CreateMonitoredEndpointDto
    {
        public Guid Id { get; set; }
    }

    public class MonitoredEndpointDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int ExpectedStatusCode { get; set; }
        public string? ContentContains { get; set; }
        public int TimeoutMs { get; set; }
        public string FrequencyCron { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Latest status (from Redis cache)
        public string CurrentStatus { get; set; } = "Unknown";
        public int? LastResponseTimeMs { get; set; }
        public double? UptimePercent { get; set; }
        public DateTime? LastCheckedAt { get; set; }
    }

    public class EndpointStatsDto
    {
        public Guid EndpointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public double UptimePercent24h { get; set; }
        public double UptimePercent7d { get; set; }
        public double UptimePercent30d { get; set; }
        public double AvgResponseMs24h { get; set; }
        public int TotalChecks24h { get; set; }
        public int Failures24h { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public int? LastResponseTimeMs { get; set; }
        public int? LastStatusCode { get; set; }
    }

    public class MonitoringLogDto
    {
        public long Id { get; set; }
        public Guid EndpointId { get; set; }
        public Guid? ProjectId { get; set; }        
        public string Status { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public int? ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    /// <summary>Real-time update pushed via SignalR.</summary>
    public class EndpointStatusUpdateDto
    {
        public Guid EndpointId { get; set; }
        public Guid ProjectId { get; set; }
        public string EndpointName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;  // Up/Down/Degraded
        public int? StatusCode { get; set; }
        public int? ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CheckedAt { get; set; }
        public bool StatusChanged { get; set; }  // true if Up→Down or Down→Up
    }
}