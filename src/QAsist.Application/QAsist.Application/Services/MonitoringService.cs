//using Microsoft.Extensions.Logging;
//using QAsist.Application.DTOs.Monitoring;
//using QAsist.Application.Interfaces.IRepositories;
//using QAsist.Application.Interfaces.IServices;
//using QAsist.Domain.Entities;
//using QAsist.Domain.Enums;
//using System.Diagnostics;
//using System.Text;

//namespace QAsist.Infrastructure.Monitoring
//{
//    /// <summary>
//    /// Core monitoring service.
//    ///
//    /// Flow (per endpoint check):
//    ///   1. Hangfire triggers job (recurring, per FrequencyCron)
//    ///   2. Acquire distributed Redis lock (prevent duplicate execution)
//    ///   3. Execute HTTP request via IHttpClientFactory
//    ///   4. Determine status: Up / Down / Degraded
//    ///   5. Save MonitoringLog to PostgreSQL
//    ///   6. Save latest status + response time to Redis (fast cache)
//    ///   7. Push real-time update via SignalR
//    ///   8. Calculate uptime% and cache in Redis
//    ///   9. Release Redis lock
//    /// </summary>
//    public class MonitoringService : IMonitoringService
//    {
//        private readonly IMonitoringRepository _monitoringRepository;
//        private readonly IRedisService _redis;
//        private readonly MonitoringNotifier _notifier;
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly ILogger<MonitoringService> _logger;

//        private const string HttpClientName = "MonitoringClient";

//        public MonitoringService(
//            IMonitoringRepository monitoringRepository,
//            IRedisService redis,
//            MonitoringNotifier notifier,
//            IHttpClientFactory httpClientFactory,
//            ILogger<MonitoringService> logger)
//        {
//            _monitoringRepository = monitoringRepository;
//            _redis = redis;
//            _notifier = notifier;
//            _httpClientFactory = httpClientFactory;
//            _logger = logger;
//        }

//        // ── CHECK ALL ENDPOINTS ───────────────────────────────────────────────
//        /// <summary>
//        /// Called by Hangfire master recurring job (every 1 minute).
//        /// Dispatches individual check jobs for each active endpoint.
//        /// </summary>
//        public async Task CheckAllEndpointsAsync(CancellationToken ct = default)
//        {
//            try
//            {
//                _logger.LogInformation(
//                    "[Monitoring] CheckAllEndpoints started at {Time}", DateTime.UtcNow);

//                var endpoints = await _monitoringRepository
//                    .GetAllActiveEndpointsAsync(ct);

//                var endpointList = endpoints.ToList();
//                _logger.LogInformation(
//                    "[Monitoring] Found {Count} active endpoints to check.", endpointList.Count);

//                // Enqueue individual check job per endpoint
//                foreach (var endpoint in endpointList)
//                {
//                    BackgroundJob.Enqueue<IMonitoringService>(
//                        s => s.CheckEndpointAsync(endpoint.Id, CancellationToken.None));
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    "[Monitoring] CheckAllEndpoints failed");
//            }
//        }

//        // ── CHECK ONE ENDPOINT ────────────────────────────────────────────────
//        /// <summary>
//        /// Checks one endpoint. Called per-endpoint by Hangfire.
//        /// Uses distributed lock to prevent duplicate execution.
//        /// </summary>
//        public async Task CheckEndpointAsync(Guid endpointId, CancellationToken ct = default)
//        {
//            var lockKey = $"monitor:check:{endpointId}";

//            // Acquire distributed lock — skip if another job is already running this
//            if (!await _redis.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
//            {
//                _logger.LogDebug(
//                    "[Monitor] Skipping endpoint {EndpointId} — lock already held.",
//                    endpointId);
//                return;
//            }

//            try
//            {
//                var endpoint = await _monitoringRepository
//                    .GetEndpointByIdAsync(endpointId, ct);

//                if (endpoint is null || !endpoint.IsActive)
//                {
//                    _logger.LogDebug(
//                        "[Monitor] Endpoint {EndpointId} not found or inactive.", endpointId);
//                    return;
//                }

//                _logger.LogDebug(
//                    "[Monitor] Checking {Name} → {Method} {Url}",
//                    endpoint.Name, endpoint.Method, endpoint.Url);

//                // Execute the HTTP check
//                var checkResult = await ExecuteCheckAsync(endpoint);

//                // Get previous status from Redis (to detect changes)
//                var previousStatus = await _redis.GetEndpointStatusAsync(endpointId);
//                var statusChanged = previousStatus is not null
//                    && previousStatus != checkResult.Status.ToString();

//                // 1. Save to PostgreSQL
//                var log = new MonitoringLog
//                {
//                    EndpointId = endpointId,
//                    ProjectId = endpoint.ProjectId,
//                    Status = checkResult.Status,
//                    StatusCode = checkResult.StatusCode,
//                    ResponseTimeMs = checkResult.ResponseTimeMs,
//                    ErrorMessage = checkResult.ErrorMessage,
//                    CheckedAt = DateTime.UtcNow
//                };

//                await _monitoringRepository.InsertLogAsync(log, CancellationToken.None);

//                // 2. Update Redis cache
//                var statusStr = checkResult.Status.ToString();
//                await _redis.SetEndpointStatusAsync(endpointId, statusStr);
//                if (checkResult.ResponseTimeMs.HasValue)
//                    await _redis.SetEndpointResponseTimeAsync(
//                        endpointId, checkResult.ResponseTimeMs.Value);

//                // 3. Calculate + cache uptime (last 24h)
//                var from = DateTime.UtcNow.AddHours(-24);
//                var to = DateTime.UtcNow;
//                var stats = await _monitoringRepository
//                    .GetUptimeStatsAsync(endpointId, from, to, CancellationToken.None);
//                await _redis.SetEndpointUptimeAsync(endpointId, stats.UptimePercent);

//                // 4. Push real-time update via SignalR
//                var update = new EndpointStatusUpdateDto
//                {
//                    EndpointId = endpointId,
//                    ProjectId = endpoint.ProjectId,
//                    EndpointName = endpoint.Name,
//                    Url = endpoint.Url,
//                    Status = statusStr,
//                    StatusCode = checkResult.StatusCode,
//                    ResponseTimeMs = checkResult.ResponseTimeMs,
//                    ErrorMessage = checkResult.ErrorMessage,
//                    CheckedAt = DateTime.UtcNow,
//                    StatusChanged = statusChanged
//                };

//                await _notifier.NotifyEndpointUpdateAsync(update);

//                _logger.LogInformation(
//                    "[Monitor] {Name} → {Status} | {Code} | {Ms}ms{Changed}",
//                    endpoint.Name, statusStr,
//                    checkResult.StatusCode, checkResult.ResponseTimeMs,
//                    statusChanged ? " [STATUS CHANGED]" : "");
//            }
//            finally
//            {
//                await _redis.ReleaseLockAsync(lockKey);
//            }
//        }

//        // ── SCHEDULE / UNSCHEDULE ─────────────────────────────────────────────
//        public async Task ScheduleEndpointAsync(Guid endpointId, string cron)
//        {
//            try
//            {
//                var jobId = $"monitor-endpoint-{endpointId}";

//                RecurringJob.AddOrUpdate<IMonitoringService>(
//                    jobId,
//                    s => s.CheckEndpointAsync(endpointId, CancellationToken.None),
//                    cron,
//                    new RecurringJobOptions
//                    {
//                        TimeZone = TimeZoneInfo.Utc
//                    });

//                await _monitoringRepository.UpdateHangfireJobIdAsync(
//                    endpointId, jobId, CancellationToken.None);

//                _logger.LogInformation(
//                    "[Monitor] Scheduled job {JobId} with cron '{Cron}'",
//                    jobId, cron);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    "[Monitor] Failed to schedule endpoint {EndpointId}", endpointId);
//                throw;
//            }
//        }

//        public async Task UnscheduleEndpointAsync(Guid endpointId)
//        {
//            try
//            {
//                var jobId = $"monitor-endpoint-{endpointId}";
//                RecurringJob.RemoveIfExists(jobId);

//                _logger.LogInformation(
//                    "[Monitor] Unscheduled job {JobId}", jobId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    "[Monitor] Failed to unschedule endpoint {EndpointId}", endpointId);
//            }

//            await Task.CompletedTask;
//        }

//        // ── PRIVATE: Execute HTTP check ───────────────────────────────────────
//        private async Task<CheckResult> ExecuteCheckAsync(MonitoredEndpoint endpoint)
//        {
//            var sw = Stopwatch.StartNew();

//            try
//            {
//                var client = _httpClientFactory.CreateClient(HttpClientName);
//                var method = new System.Net.Http.HttpMethod(endpoint.Method);
//                var request = new HttpRequestMessage(method, endpoint.Url);

//                // Add configured headers
//                foreach (var (key, value) in endpoint.RequestHeaders)
//                    request.Headers.TryAddWithoutValidation(key, value);

//                // Add body if configured
//                if (!string.IsNullOrWhiteSpace(endpoint.RequestBody)
//                    && method.Method is "POST" or "PUT" or "PATCH")
//                {
//                    request.Content = new StringContent(
//                        endpoint.RequestBody, Encoding.UTF8, "application/json");
//                }

//                using var cts = new CancellationTokenSource(
//                    TimeSpan.FromMilliseconds(endpoint.TimeoutMs));

//                var response = await client.SendAsync(request, cts.Token);
//                sw.Stop();

//                var statusCode = (int)response.StatusCode;
//                var body = await response.Content.ReadAsStringAsync();

//                // Determine status
//                var status = DetermineStatus(
//                    statusCode, body, sw.ElapsedMilliseconds,
//                    endpoint.ExpectedStatusCode,
//                    endpoint.ContentContains,
//                    endpoint.TimeoutMs);

//                return new CheckResult
//                {
//                    Status = status,
//                    StatusCode = statusCode,
//                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
//                    ErrorMessage = status == MonitorStatus.Down
//                        ? BuildFailureMessage(statusCode, body, endpoint)
//                        : null
//                };
//            }
//            catch (TaskCanceledException)
//            {
//                sw.Stop();
//                return new CheckResult
//                {
//                    Status = MonitorStatus.Down,
//                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
//                    ErrorMessage = $"Timeout after {endpoint.TimeoutMs}ms"
//                };
//            }
//            catch (HttpRequestException ex)
//            {
//                sw.Stop();
//                return new CheckResult
//                {
//                    Status = MonitorStatus.Down,
//                    ErrorMessage = $"Network error: {ex.Message}"
//                };
//            }
//            catch (Exception ex)
//            {
//                sw.Stop();
//                return new CheckResult
//                {
//                    Status = MonitorStatus.Down,
//                    ErrorMessage = $"Error: {ex.Message}"
//                };
//            }
//        }

//        private static MonitorStatus DetermineStatus(
//            int statusCode,
//            string body,
//            long responseTimeMs,
//            int expectedStatusCode,
//            string? contentContains,
//            int timeoutMs)
//        {
//            // Wrong status code = Down
//            if (statusCode != expectedStatusCode)
//                return MonitorStatus.Down;

//            // Body check failed = Down
//            if (!string.IsNullOrWhiteSpace(contentContains)
//                && !body.Contains(contentContains, StringComparison.OrdinalIgnoreCase))
//                return MonitorStatus.Down;

//            // Slow response (> 80% of timeout) = Degraded
//            if (responseTimeMs > timeoutMs * 0.8)
//                return MonitorStatus.Degraded;

//            return MonitorStatus.Up;
//        }

//        private static string BuildFailureMessage(
//            int statusCode, string body, MonitoredEndpoint endpoint)
//        {
//            if (statusCode != endpoint.ExpectedStatusCode)
//                return $"Expected status {endpoint.ExpectedStatusCode}, got {statusCode}";

//            if (!string.IsNullOrWhiteSpace(endpoint.ContentContains)
//                && !body.Contains(endpoint.ContentContains,
//                    StringComparison.OrdinalIgnoreCase))
//                return $"Response body does not contain '{endpoint.ContentContains}'";

//            return "Unknown failure";
//        }

//        private sealed class CheckResult
//        {
//            public MonitorStatus Status { get; init; }
//            public int? StatusCode { get; init; }
//            public int? ResponseTimeMs { get; init; }
//            public string? ErrorMessage { get; init; }
//        }
//    }
//}

using Hangfire;
using Microsoft.Extensions.Logging;
using QAsist.Application.DTOs.Monitoring;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using System.Diagnostics;
using System.Text;

namespace QAsist.Infrastructure.Monitoring
{
    public class MonitoringService : IMonitoringService
    {
        private readonly IMonitoringRepository _monitoringRepository;
        private readonly IRedisService _redis;
        private readonly IMonitoringNotifier _notifier;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MonitoringService> _logger;

        private const string HttpClientName = "MonitoringClient";

        public MonitoringService(
            IMonitoringRepository monitoringRepository,
            IRedisService redis,
            IMonitoringNotifier notifier,
            IHttpClientFactory httpClientFactory,
            ILogger<MonitoringService> logger)
        {
            _monitoringRepository = monitoringRepository;
            _redis = redis;
            _notifier = notifier;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // CHECK ALL ENDPOINTS
        // ─────────────────────────────────────────────────────────────
        public async Task CheckAllEndpointsAsync(CancellationToken ct = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                _logger.LogInformation("[Monitoring] Started at {Time}", now);

                var endpoints = (await _monitoringRepository
                    .GetAllActiveEndpointsAsync(ct)).ToList();

                _logger.LogInformation("[Monitoring] Found {Count} endpoints", endpoints.Count);

                // Limit parallel jobs (avoid explosion)
                await Parallel.ForEachAsync(endpoints, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = ct
                },
                async (endpoint, token) =>
                {
                    await CheckEndpointAsync(endpoint.Id, token);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] CheckAllEndpoints failed");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // CHECK SINGLE ENDPOINT
        // ─────────────────────────────────────────────────────────────
        public async Task CheckEndpointAsync(Guid endpointId, CancellationToken ct = default)
        {
            var lockKey = $"monitor:check:{endpointId}";

            if (!await _redis.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
            {
                _logger.LogDebug("[Monitor] Skipped {EndpointId} (locked)", endpointId);
                return;
            }

            try
            {
                var endpoint = await _monitoringRepository
                    .GetEndpointByIdAsync(endpointId, ct);

                if (endpoint is null || !endpoint.IsActive)
                    return;

                var result = await ExecuteCheckWithRetryAsync(endpoint, ct);

                var previousStatus = await _redis.GetEndpointStatusAsync(endpointId);
                var statusStr = result.Status.ToString();

                var statusChanged = previousStatus != null && previousStatus != statusStr;

                var now = DateTime.UtcNow;

                // Save log
                await _monitoringRepository.InsertLogAsync(new MonitoringLog
                {
                    EndpointId = endpointId,
                    ProjectId = endpoint.ProjectId,
                    Status = result.Status,
                    StatusCode = result.StatusCode,
                    ResponseTimeMs = result.ResponseTimeMs,
                    ErrorMessage = result.ErrorMessage,
                    CheckedAt = now
                }, ct);

                // Redis cache
                await _redis.SetEndpointStatusAsync(endpointId, statusStr);
                if (result.ResponseTimeMs.HasValue)
                    await _redis.SetEndpointResponseTimeAsync(endpointId, result.ResponseTimeMs.Value);

                // Uptime calc (24h)
                var stats = await _monitoringRepository.GetUptimeStatsAsync(
                    endpointId,
                    now.AddHours(-24),
                    now,
                    ct);

                await _redis.SetEndpointUptimeAsync(endpointId, stats.UptimePercent);

                // SignalR push
                await _notifier.NotifyEndpointUpdateAsync(new EndpointStatusUpdateDto
                {
                    EndpointId = endpointId,
                    ProjectId = endpoint.ProjectId,
                    EndpointName = endpoint.Name,
                    Url = endpoint.Url,
                    Status = statusStr,
                    StatusCode = result.StatusCode,
                    ResponseTimeMs = result.ResponseTimeMs,
                    ErrorMessage = result.ErrorMessage,
                    CheckedAt = now,
                    StatusChanged = statusChanged
                });

                _logger.LogInformation(
                    "[Monitor] {Name} → {Status} | {Code} | {Ms}ms",
                    endpoint.Name, statusStr, result.StatusCode, result.ResponseTimeMs);
            }
            finally
            {
                await _redis.ReleaseLockAsync(lockKey);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RETRY LOGIC
        // ─────────────────────────────────────────────────────────────
        private async Task<CheckResult> ExecuteCheckWithRetryAsync(
            MonitoredEndpoint endpoint,
            CancellationToken ct)
        {
            const int maxRetries = 2;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await ExecuteCheckAsync(endpoint, ct);
                }
                catch when (i < maxRetries)
                {
                    await Task.Delay(200, ct);
                }
            }

            return new CheckResult
            {
                Status = MonitorStatus.Down,
                ErrorMessage = "Failed after retries"
            };
        }

        // ─────────────────────────────────────────────────────────────
        // HTTP EXECUTION
        // ─────────────────────────────────────────────────────────────
        private async Task<CheckResult> ExecuteCheckAsync(
            MonitoredEndpoint endpoint,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);

                var request = new HttpRequestMessage(
    new System.Net.Http.HttpMethod(endpoint.Method),
    endpoint.Url);

                foreach (var (k, v) in endpoint.RequestHeaders)
                    request.Headers.TryAddWithoutValidation(k, v);

                if (!string.IsNullOrWhiteSpace(endpoint.RequestBody) &&
                    endpoint.Method is "POST" or "PUT" or "PATCH")
                {
                    request.Content = new StringContent(
                        endpoint.RequestBody, Encoding.UTF8, "application/json");
                }

                using var timeoutCts = new CancellationTokenSource(
                    TimeSpan.FromMilliseconds(endpoint.TimeoutMs));

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    ct, timeoutCts.Token);

                var response = await client.SendAsync(request, linkedCts.Token);

                sw.Stop();

                var body = await response.Content.ReadAsStringAsync(ct);
                var statusCode = (int)response.StatusCode;

                var status = DetermineStatus(
                    statusCode,
                    body,
                    sw.ElapsedMilliseconds,
                    endpoint.ExpectedStatusCode,
                    endpoint.ContentContains,
                    endpoint.TimeoutMs);

                return new CheckResult
                {
                    Status = status,
                    StatusCode = statusCode,
                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                    ErrorMessage = status == MonitorStatus.Down
                        ? $"Expected {endpoint.ExpectedStatusCode}, got {statusCode}"
                        : null
                };
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                return new CheckResult
                {
                    Status = MonitorStatus.Down,
                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                    ErrorMessage = $"Timeout ({endpoint.TimeoutMs}ms)"
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new CheckResult
                {
                    Status = MonitorStatus.Down,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ─────────────────────────────────────────────────────────────
        // STATUS LOGIC
        // ─────────────────────────────────────────────────────────────
        private static MonitorStatus DetermineStatus(
            int statusCode,
            string body,
            long responseTimeMs,
            int expectedStatusCode,
            string? contentContains,
            int timeoutMs)
        {
            if (statusCode != expectedStatusCode)
                return MonitorStatus.Down;

            if (!string.IsNullOrWhiteSpace(contentContains) &&
                !body.Contains(contentContains, StringComparison.OrdinalIgnoreCase))
                return MonitorStatus.Down;

            if (responseTimeMs > timeoutMs * 0.8)
                return MonitorStatus.Degraded;

            return MonitorStatus.Up;
        }

        private sealed class CheckResult
        {
            public MonitorStatus Status { get; init; }
            public int? StatusCode { get; init; }
            public int? ResponseTimeMs { get; init; }
            public string? ErrorMessage { get; init; }
        }

        // ─────────────────────────────────────────────────────────────
        // SCHEDULING
        // ─────────────────────────────────────────────────────────────
        public async Task ScheduleEndpointAsync(Guid endpointId, string cron)
        {
            var jobId = $"monitor-endpoint-{endpointId}";

            RecurringJob.AddOrUpdate<IMonitoringService>(
                jobId,
                s => s.CheckEndpointAsync(endpointId, CancellationToken.None),
                cron);

            await _monitoringRepository.UpdateHangfireJobIdAsync(
                endpointId, jobId, CancellationToken.None);
        }

        public Task UnscheduleEndpointAsync(Guid endpointId)
        {
            RecurringJob.RemoveIfExists($"monitor-endpoint-{endpointId}");
            return Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────
        // CRUD (keep existing implementation)
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<MonitoredEndpointDto>> GetEndpointsByProjectAsync(
       Guid projectId, CancellationToken ct = default)
        {
            var endpoints = await _monitoringRepository
                .GetEndpointsByProjectAsync(projectId, ct);

            var tasks = endpoints.Select(e => MapToDtoAsync(e, ct));

            return await Task.WhenAll(tasks);
        }

        public async Task<MonitoredEndpointDto> GetEndpointByIdAsync(
      Guid id, CancellationToken ct = default)
        {
            var endpoint = await _monitoringRepository
                .GetEndpointByIdAsync(id, ct);

            if (endpoint == null)
                throw new Exception("Endpoint not found");

            return await MapToDtoAsync(endpoint, ct);
        }

        public async Task<MonitoredEndpointDto> CreateEndpointAsync(
    CreateMonitoredEndpointDto dto, Guid userId, CancellationToken ct = default)
        {
            var entity = new MonitoredEndpoint
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                Url = dto.Url,
                Method = dto.Method,
                RequestHeaders = dto.RequestHeaders ?? new(),
                RequestBody = dto.RequestBody,
                ExpectedStatusCode = dto.ExpectedStatusCode,
                ContentContains = dto.ContentContains,
                TimeoutMs = dto.TimeoutMs,
                FrequencyCron = dto.FrequencyCron,
                IsActive = true
            };

            var id = await _monitoringRepository
                .CreateEndpointAsync(entity, userId, ct);

            entity.Id = id;

            return await MapToDtoAsync(entity, ct);
        }

        public async Task<MonitoredEndpointDto> UpdateEndpointAsync(
    UpdateMonitoredEndpointDto dto, Guid userId, CancellationToken ct = default)
        {
            var existing = await _monitoringRepository
                .GetEndpointByIdAsync(dto.Id, ct);

            if (existing == null)
                throw new Exception("Endpoint not found");

            existing.Name = dto.Name;
            existing.Url = dto.Url;
            existing.Method = dto.Method;
            existing.RequestHeaders = dto.RequestHeaders ?? new();
            existing.RequestBody = dto.RequestBody;
            existing.ExpectedStatusCode = dto.ExpectedStatusCode;
            existing.ContentContains = dto.ContentContains;
            existing.TimeoutMs = dto.TimeoutMs;
            existing.FrequencyCron = dto.FrequencyCron;
            existing.IsActive = dto.IsActive;

            await _monitoringRepository.UpdateEndpointAsync(existing, userId, ct);

            return await MapToDtoAsync(existing, ct);
        }

        public async Task DeleteEndpointAsync(
            Guid id, Guid userId, CancellationToken ct = default)
        {
            var success = await _monitoringRepository
                .DeleteEndpointAsync(id, userId, ct);

            if (!success)
                throw new Exception("Delete failed or endpoint not found");
        }

        public async Task<(IEnumerable<MonitoringLogDto> Logs, int Total)> GetLogsPagedAsync(
    Guid endpointId, int page, int size, CancellationToken ct = default)
        {
            var (logs, total) = await _monitoringRepository
                .GetLogsPagedAsync(endpointId, page, size, ct);

            var result = logs.Select(l => new MonitoringLogDto
            {
                Id = l.Id,
                EndpointId = l.EndpointId,
                ProjectId = l.ProjectId,
                Status = l.Status.ToString(),
                StatusCode = l.StatusCode,
                ResponseTimeMs = l.ResponseTimeMs,
                ErrorMessage = l.ErrorMessage,
                CheckedAt = l.CheckedAt
            });

            return (result, total);
        }
        private async Task<MonitoredEndpointDto> MapToDtoAsync(
    MonitoredEndpoint e,
    CancellationToken ct = default)
        {
            var status = await _redis.GetEndpointStatusAsync(e.Id);
            var responseTime = await _redis.GetEndpointResponseTimeAsync(e.Id);
            var uptime = await _redis.GetEndpointUptimeAsync(e.Id);

            return new MonitoredEndpointDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                Name = e.Name,
                Url = e.Url,
                Method = e.Method,
                ExpectedStatusCode = e.ExpectedStatusCode,
                ContentContains = e.ContentContains,
                TimeoutMs = e.TimeoutMs,
                FrequencyCron = e.FrequencyCron,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,

                // 🔥 Redis live data
                CurrentStatus = status ?? "Unknown",
                LastResponseTimeMs = responseTime,
                UptimePercent = uptime
            };
        }
    }
}