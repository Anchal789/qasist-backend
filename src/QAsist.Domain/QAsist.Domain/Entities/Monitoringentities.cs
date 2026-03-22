using QAsist.Domain.Enums;

namespace QAsist.Domain.Entities
{
    /// <summary>
    /// T7 — An API endpoint registered for scheduled health monitoring.
    /// Maps to: monitored_endpoints table (NEW table).
    /// 
    /// Each active endpoint gets a Hangfire recurring job registered at
    /// the configured FrequencyCron interval.
    /// Job ID = endpoint ID (string) — used for job lifecycle management.
    /// </summary>
    public class MonitoredEndpoint : BaseEntity
    {
        public Guid ProjectId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        // ── HTTP Configuration ────────────────────────────────────────────────
        /// <summary>HTTP method for the check. Usually GET.</summary>
        public string Method { get; set; } = "GET";

        /// <summary>Optional request headers as JSONB key-value.</summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = new();

        /// <summary>Optional request body (for POST/PUT monitors).</summary>
        public string? RequestBody { get; set; }

        // ── Validation Rules ──────────────────────────────────────────────────
        /// <summary>Expected HTTP status code. Default: 200.</summary>
        public int ExpectedStatusCode { get; set; } = 200;

        /// <summary>
        /// Optional substring that MUST appear in response body.
        /// If null: only status code is checked.
        /// Case-insensitive match.
        /// </summary>
        public string? ContentContains { get; set; }

        /// <summary>Request timeout in milliseconds. Default: 10000 (10s).</summary>
        public int TimeoutMs { get; set; } = 10_000;

        // ── Schedule ──────────────────────────────────────────────────────────
        /// <summary>
        /// Cron expression for Hangfire recurring job.
        /// Common values:
        ///   "*/1 * * * *"  = every 1 minute
        ///   "*/5 * * * *"  = every 5 minutes
        ///   "*/15 * * * *" = every 15 minutes
        ///   "0 * * * *"    = every hour
        /// </summary>
        public string FrequencyCron { get; set; } = "*/5 * * * *";

        public bool IsActive { get; set; } = true;

        /// <summary>Hangfire job ID for this endpoint. Set on create/update.</summary>
        public string? HangfireJobId { get; set; }

        // ── Alert Configuration (JSONB) ───────────────────────────────────────
        /// <summary>
        /// Serialized alert config JSON.
        /// Deserialized to AlertConfig value object by the monitoring service.
        /// Example: {"failureThreshold":3,"email":{"enabled":true,"recipients":["ops@co.com"]}}
        /// </summary>
        public string? AlertConfigJson { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        public Project Project { get; set; } = null!;
        public ICollection<MonitoringLog> Logs { get; set; } = new List<MonitoringLog>();
    }

    /// <summary>
    /// T8 — Single result from a monitoring check run.
    /// Maps to: monitoring_logs table (NEW table).
    /// 
    /// HIGH VOLUME table — uses long (BIGINT SERIAL) PK instead of UUID.
    /// Expected: thousands of rows per day per active endpoint.
    /// Index: (endpoint_id, checked_at DESC) for time-range queries.
    /// Consider partitioning by month when row count exceeds 10M.
    /// </summary>
    public class MonitoringLog
    {
        /// <summary>BIGINT SERIAL — auto-increment, not UUID (high volume table).</summary>
        public long Id { get; set; }

        public Guid EndpointId { get; set; }
        public Guid ProjectId { get; set; }

        public MonitorStatus Status { get; set; }         // UP / DOWN / DEGRADED

        public int? StatusCode { get; set; }
        public int? ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>UTC timestamp of when the check ran.</summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ────────────────────────────────────────────────────────
        public MonitoredEndpoint Endpoint { get; set; } = null!;
    }
}