using Microsoft.Extensions.Logging;
using QAsist.Application.DTOs.Monitoring;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;

namespace QAsist.Application.Services
{
    public class UptimeCalculator : IUptimeCalculator
    {
        private readonly IMonitoringRepository _monitoringRepository;
        private readonly IRedisService _redis;
        private readonly ILogger<UptimeCalculator> _logger;

        public UptimeCalculator(
            IMonitoringRepository monitoringRepository,
            IRedisService redis,
            ILogger<UptimeCalculator> logger)
        {
            _monitoringRepository = monitoringRepository;
            _redis = redis;
            _logger = logger;
        }

        public async Task<EndpointStatsDto> GetStatsAsync(
            Guid endpointId,
            CancellationToken ct = default)
        {
            try
            {
                var endpoint = await _monitoringRepository
                    .GetEndpointByIdAsync(endpointId, ct);

                if (endpoint is null)
                    return new EndpointStatsDto { EndpointId = endpointId };

                var now = DateTime.UtcNow;

                // Calculate for 24h, 7d, 30d windows in parallel
                var task24h = _monitoringRepository.GetUptimeStatsAsync(
                    endpointId, now.AddHours(-24), now, ct);
                var task7d = _monitoringRepository.GetUptimeStatsAsync(
                    endpointId, now.AddDays(-7), now, ct);
                var task30d = _monitoringRepository.GetUptimeStatsAsync(
                    endpointId, now.AddDays(-30), now, ct);

                await Task.WhenAll(task24h, task7d, task30d);

                var stats24h = await task24h;
                var stats7d = await task7d;
                var stats30d = await task30d;

                // Get latest values from Redis cache
                var currentStatus = await _redis.GetEndpointStatusAsync(endpointId) ?? "Unknown";
                var lastMs = await _redis.GetEndpointResponseTimeAsync(endpointId);

                // Get last log entry for last checked timestamp
                var recentLogs = await _monitoringRepository
                    .GetLogsInRangeAsync(endpointId, now.AddMinutes(-10), now, ct);
                var lastLog = recentLogs.OrderByDescending(l => l.CheckedAt).FirstOrDefault();

                var dto = new EndpointStatsDto
                {
                    EndpointId = endpointId,
                    Name = endpoint.Name,
                    Url = endpoint.Url,
                    CurrentStatus = currentStatus,
                    UptimePercent24h = stats24h.UptimePercent,
                    UptimePercent7d = stats7d.UptimePercent,
                    UptimePercent30d = stats30d.UptimePercent,
                    AvgResponseMs24h = stats24h.AvgResponseMs,
                    TotalChecks24h = stats24h.TotalChecks,
                    Failures24h = stats24h.Failures,
                    LastCheckedAt = lastLog?.CheckedAt,
                    LastResponseTimeMs = lastMs ?? lastLog?.ResponseTimeMs,
                    LastStatusCode = lastLog?.StatusCode
                };

                // Update Redis with fresh uptime
                await _redis.SetEndpointUptimeAsync(endpointId, stats24h.UptimePercent);

                _logger.LogDebug(
                    "Stats for {EndpointId}: Uptime24h={U24}% Uptime7d={U7}%",
                    endpointId, stats24h.UptimePercent, stats7d.UptimePercent);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error calculating uptime stats for endpoint {EndpointId}", endpointId);
                throw;
            }
        }
    }
}