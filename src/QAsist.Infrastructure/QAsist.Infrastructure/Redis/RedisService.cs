using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IServices;
using StackExchange.Redis;

namespace QAsist.Infrastructure.Redis
{
    /// <summary>
    /// Redis cache for real-time endpoint monitoring status.
    ///
    /// Cloud Config:
    ///   Host:     redis-16206.crce217.ap-south-1-1.ec2.cloud.redislabs.com
    ///   Port:     16206
    ///   User:     default
    ///   Password: D6WDrQAnDoQaZmyOsfyt84qoaFSWBHRp
    ///   Region:   AWS ap-south-1 (Mumbai)
    ///
    /// Key design:
    ///   endpoint:{id}:status       → "Up" / "Down" / "Degraded"
    ///   endpoint:{id}:responseTime → "142" (ms as string)
    ///   endpoint:{id}:uptime       → "99.87" (% as string)
    ///   lock:{key}                 → "1" (distributed lock)
    /// </summary>
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisService> _logger;

        // Default TTL for status keys (25 minutes — slightly above max check interval)
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(25);

        public RedisService(
            IConnectionMultiplexer redis,
            ILogger<RedisService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
        }

        // ── Status ────────────────────────────────────────────────────────────
        public async Task SetEndpointStatusAsync(
            Guid endpointId, string status, TimeSpan? expiry = null)
        {
            try
            {
                var key = EndpointKey(endpointId, "status");
                await _db.StringSetAsync(key, status, expiry ?? DefaultExpiry);
                _logger.LogDebug("Redis SET {Key} = {Status}", key, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis write failed for endpoint {EndpointId} status", endpointId);
                // Never throw — Redis failure must not break monitoring
            }
        }

        public async Task<string?> GetEndpointStatusAsync(Guid endpointId)
        {
            try
            {
                var value = await _db.StringGetAsync(EndpointKey(endpointId, "status"));
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis read failed for endpoint {EndpointId} status", endpointId);
                return null;
            }
        }

        // ── Response Time ─────────────────────────────────────────────────────
        public async Task SetEndpointResponseTimeAsync(
            Guid endpointId, int responseTimeMs, TimeSpan? expiry = null)
        {
            try
            {
                await _db.StringSetAsync(
                    EndpointKey(endpointId, "responseTime"),
                    responseTimeMs.ToString(),
                    expiry ?? DefaultExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis write failed for endpoint {EndpointId} responseTime", endpointId);
            }
        }

        public async Task<int?> GetEndpointResponseTimeAsync(Guid endpointId)
        {
            try
            {
                var value = await _db.StringGetAsync(
                    EndpointKey(endpointId, "responseTime"));
                return value.HasValue && int.TryParse(value, out var ms) ? ms : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis read failed for endpoint {EndpointId} responseTime", endpointId);
                return null;
            }
        }

        // ── Uptime ────────────────────────────────────────────────────────────
        public async Task SetEndpointUptimeAsync(
            Guid endpointId, double uptimePercent, TimeSpan? expiry = null)
        {
            try
            {
                await _db.StringSetAsync(
                    EndpointKey(endpointId, "uptime"),
                    uptimePercent.ToString("F2"),
                    expiry ?? TimeSpan.FromHours(1)); // uptime cached longer
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis write failed for endpoint {EndpointId} uptime", endpointId);
            }
        }

        public async Task<double?> GetEndpointUptimeAsync(Guid endpointId)
        {
            try
            {
                var value = await _db.StringGetAsync(
                    EndpointKey(endpointId, "uptime"));
                return value.HasValue && double.TryParse(value.ToString(), out var pct)
                    ? pct : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis read failed for endpoint {EndpointId} uptime", endpointId);
                return null;
            }
        }

        // ── Distributed Lock (SETNX pattern) ─────────────────────────────────
        /// <summary>
        /// Acquires a distributed lock to prevent duplicate job execution.
        /// Uses SETNX (SET if Not eXists) — atomic operation.
        /// </summary>
        public async Task<bool> AcquireLockAsync(string lockKey, TimeSpan lockDuration)
        {
            try
            {
                var key = $"lock:{lockKey}";
                var acquired = await _db.StringSetAsync(
                    key, "1", lockDuration,
                    When.NotExists); // SETNX — only set if key does NOT exist

                _logger.LogDebug(
                    "Lock {Key}: {Result}", key, acquired ? "ACQUIRED" : "ALREADY HELD");

                return acquired;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis lock acquisition failed for key {LockKey}", lockKey);
                // Fail open — allow execution if Redis is down
                return true;
            }
        }

        public async Task ReleaseLockAsync(string lockKey)
        {
            try
            {
                await _db.KeyDeleteAsync($"lock:{lockKey}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis lock release failed for key {LockKey}", lockKey);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static RedisKey EndpointKey(Guid endpointId, string field) =>
            $"endpoint:{endpointId}:{field}";
    }
}