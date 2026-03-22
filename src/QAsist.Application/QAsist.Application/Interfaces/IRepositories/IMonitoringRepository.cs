using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface IMonitoringRepository
    {
        // Endpoints
        Task<MonitoredEndpoint?> GetEndpointByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<MonitoredEndpoint>> GetEndpointsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<Guid> CreateEndpointAsync(MonitoredEndpoint endpoint, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateEndpointAsync(MonitoredEndpoint endpoint, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteEndpointAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

        // Logs
        Task InsertLogAsync(MonitoringLog log, CancellationToken cancellationToken = default);

        Task<(IEnumerable<MonitoringLog> Logs, int TotalCount)> GetLogsPagedAsync(
            Guid endpointId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Returns logs within a time range for uptime calculation.</summary>
        Task<IEnumerable<MonitoringLog>> GetLogsInRangeAsync(
            Guid endpointId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

        /// <summary>Returns uptime % and avg response time for a given range.</summary>
        Task<(double UptimePercent, double AvgResponseMs, int TotalChecks, int Failures)> GetUptimeStatsAsync(
            Guid endpointId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }
}
