using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
  public interface IMonitoringRepository
    {
        // ── Endpoints ─────────────────────────────────────────────────────────
        Task<MonitoredEndpoint?> GetEndpointByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<MonitoredEndpoint>> GetEndpointsByProjectAsync(Guid projectId, CancellationToken ct = default);
        Task<IEnumerable<MonitoredEndpoint>> GetAllActiveEndpointsAsync(CancellationToken ct = default);
        Task<Guid> CreateEndpointAsync(MonitoredEndpoint endpoint, Guid userId, CancellationToken ct = default);
        Task<bool> UpdateEndpointAsync(MonitoredEndpoint endpoint, Guid userId, CancellationToken ct = default);
        Task<bool> DeleteEndpointAsync(Guid id, Guid userId, CancellationToken ct = default);
        Task UpdateHangfireJobIdAsync(Guid id, string jobId, CancellationToken ct = default);
 
        // ── Logs ──────────────────────────────────────────────────────────────
        Task InsertLogAsync(MonitoringLog log, CancellationToken ct = default);
        Task<(IEnumerable<MonitoringLog> Logs, int Total)> GetLogsPagedAsync(Guid endpointId, int page, int size, CancellationToken ct = default);
        Task<IEnumerable<MonitoringLog>> GetLogsInRangeAsync(Guid endpointId, DateTime from, DateTime to, CancellationToken ct = default);
        Task<(double UptimePercent, double AvgResponseMs, int TotalChecks, int Failures)> GetUptimeStatsAsync(Guid endpointId, DateTime from, DateTime to, CancellationToken ct = default);
    }
}
