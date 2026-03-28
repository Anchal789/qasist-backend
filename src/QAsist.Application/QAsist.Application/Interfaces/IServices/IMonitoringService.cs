using QAsist.Application.DTOs.Monitoring;

namespace QAsist.Application.Interfaces.IServices
{
    /// <summary>Redis cache for real-time endpoint status.</summary>
    public interface IRedisService
    {
        // Status cache
        Task SetEndpointStatusAsync(Guid endpointId, string status, TimeSpan? expiry = null);
        Task<string?> GetEndpointStatusAsync(Guid endpointId);
        Task SetEndpointResponseTimeAsync(Guid endpointId, int responseTimeMs, TimeSpan? expiry = null);
        Task<int?> GetEndpointResponseTimeAsync(Guid endpointId);
        Task SetEndpointUptimeAsync(Guid endpointId, double uptimePercent, TimeSpan? expiry = null);
        Task<double?> GetEndpointUptimeAsync(Guid endpointId);

        // Distributed lock (prevent duplicate Hangfire job execution)
        Task<bool> AcquireLockAsync(string lockKey, TimeSpan lockDuration);
        Task ReleaseLockAsync(string lockKey);
    }

    /// <summary>Core monitoring service — checks endpoints and processes results.</summary>
    public interface IMonitoringService
    {
        // ── CRUD (called by controller) ───────────────────────────────────────
        Task<IEnumerable<MonitoredEndpointDto>> GetEndpointsByProjectAsync(
            Guid projectId, CancellationToken ct = default);

        Task<MonitoredEndpointDto> GetEndpointByIdAsync(
            Guid id, CancellationToken ct = default);

        Task<MonitoredEndpointDto> CreateEndpointAsync(
            CreateMonitoredEndpointDto dto, Guid userId, CancellationToken ct = default);

        Task<MonitoredEndpointDto> UpdateEndpointAsync(
            UpdateMonitoredEndpointDto dto, Guid userId, CancellationToken ct = default);

        Task DeleteEndpointAsync(Guid id, Guid userId, CancellationToken ct = default);

        Task<(IEnumerable<MonitoringLogDto> Logs, int Total)> GetLogsPagedAsync(
            Guid endpointId, int page, int size, CancellationToken ct = default);

        // ── Execution (called by Hangfire + manual trigger) ───────────────────
        Task CheckAllEndpointsAsync(CancellationToken ct = default);
        Task CheckEndpointAsync(Guid endpointId, CancellationToken ct = default);

        // ── Scheduling ────────────────────────────────────────────────────────
        Task ScheduleEndpointAsync(Guid endpointId, string cron);
        Task UnscheduleEndpointAsync(Guid endpointId);
    }

    /// <summary>Calculates uptime statistics from monitoring logs.</summary>
    public interface IUptimeCalculator
    {
        Task<EndpointStatsDto> GetStatsAsync(Guid endpointId, CancellationToken ct = default);
    }
}