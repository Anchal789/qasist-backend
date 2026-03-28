using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Infrastructure.Persistence;
using System.Text.Json;

namespace QAsist.Infrastructure.Repository
{
    public class MonitoringRepository : IMonitoringRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<MonitoringRepository> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public MonitoringRepository(
            IDbConnectionFactory connectionFactory,
            ILogger<MonitoringRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ── GET ENDPOINT BY ID ────────────────────────────────────────────────
        public async Task<MonitoredEndpoint?> GetEndpointByIdAsync(
            Guid id, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                const string sql = @"
                    SELECT id, project_id, name, url, method, request_headers,
                           request_body, expected_status_code, content_contains,
                           timeout_ms, frequency_cron, is_active, hangfire_job_id,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM monitored_endpoints
                    WHERE id = @Id AND is_deleted = false";

                var db = await connection.QueryFirstOrDefaultAsync<MonitoredEndpointDb>(
                    sql, new { Id = id });
                return db is null ? null : MapToEntity(db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetEndpointByIdAsync {Id}", id);
                throw;
            }
        }

        // ── GET BY PROJECT ────────────────────────────────────────────────────
        public async Task<IEnumerable<MonitoredEndpoint>> GetEndpointsByProjectAsync(
            Guid projectId, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                const string sql = @"
                    SELECT id, project_id, name, url, method, request_headers,
                           request_body, expected_status_code, content_contains,
                           timeout_ms, frequency_cron, is_active, hangfire_job_id,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM monitored_endpoints
                    WHERE project_id = @ProjectId AND is_deleted = false
                    ORDER BY created_at DESC";

                var results = await connection.QueryAsync<MonitoredEndpointDb>(
                    sql, new { ProjectId = projectId });
                return results.Select(MapToEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetEndpointsByProjectAsync {ProjectId}", projectId);
                throw;
            }
        }

        // ── GET ALL ACTIVE ────────────────────────────────────────────────────
        public async Task<IEnumerable<MonitoredEndpoint>> GetAllActiveEndpointsAsync(
            CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                const string sql = @"
                    SELECT id, project_id, name, url, method, request_headers,
                           request_body, expected_status_code, content_contains,
                           timeout_ms, frequency_cron, is_active, hangfire_job_id,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM monitored_endpoints
                    WHERE is_active = true AND is_deleted = false";

                var results = await connection.QueryAsync<MonitoredEndpointDb>(sql);
                return results.Select(MapToEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repository error: GetAllActiveEndpointsAsync");
                throw;
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<Guid> CreateEndpointAsync(
            MonitoredEndpoint endpoint, Guid userId, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                var npgsql = (NpgsqlConnection)connection;

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO monitored_endpoints
                        (id, project_id, name, url, method, request_headers,
                         request_body, expected_status_code, content_contains,
                         timeout_ms, frequency_cron, is_active,
                         created_by, created_at)
                    VALUES
                        (@id, @project_id, @name, @url, @method, @request_headers,
                         @request_body, @expected_status_code, @content_contains,
                         @timeout_ms, @frequency_cron, @is_active,
                         @created_by, NOW())
                    RETURNING id";

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, endpoint.Id);
                cmd.Parameters.AddWithValue("project_id", NpgsqlDbType.Uuid, endpoint.ProjectId);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, endpoint.Name);
                cmd.Parameters.AddWithValue("url", NpgsqlDbType.Text, endpoint.Url);
                cmd.Parameters.AddWithValue("method", NpgsqlDbType.Varchar, endpoint.Method);
                cmd.Parameters.AddWithValue("request_headers", NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(endpoint.RequestHeaders, _jsonOptions));
                cmd.Parameters.AddWithValue("request_body", NpgsqlDbType.Text,
                    (object?)endpoint.RequestBody ?? DBNull.Value);
                cmd.Parameters.AddWithValue("expected_status_code",
                    NpgsqlDbType.Integer, endpoint.ExpectedStatusCode);
                cmd.Parameters.AddWithValue("content_contains", NpgsqlDbType.Text,
                    (object?)endpoint.ContentContains ?? DBNull.Value);
                cmd.Parameters.AddWithValue("timeout_ms", NpgsqlDbType.Integer, endpoint.TimeoutMs);
                cmd.Parameters.AddWithValue("frequency_cron", NpgsqlDbType.Varchar, endpoint.FrequencyCron);
                cmd.Parameters.AddWithValue("is_active", NpgsqlDbType.Boolean, endpoint.IsActive);
                cmd.Parameters.AddWithValue("created_by", NpgsqlDbType.Uuid, userId);

                var result = await cmd.ExecuteScalarAsync(ct);
                return (Guid)result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: CreateEndpointAsync {Name}", endpoint.Name);
                throw;
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<bool> UpdateEndpointAsync(
            MonitoredEndpoint endpoint, Guid userId, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                var npgsql = (NpgsqlConnection)connection;

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = @"
                    UPDATE monitored_endpoints SET
                        name                 = @name,
                        url                  = @url,
                        method               = @method,
                        request_headers      = @request_headers,
                        request_body         = @request_body,
                        expected_status_code = @expected_status_code,
                        content_contains     = @content_contains,
                        timeout_ms           = @timeout_ms,
                        frequency_cron       = @frequency_cron,
                        is_active            = @is_active,
                        updated_by           = @updated_by,
                        updated_at           = NOW()
                    WHERE id = @id AND is_deleted = false
                    RETURNING TRUE";

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, endpoint.Id);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, endpoint.Name);
                cmd.Parameters.AddWithValue("url", NpgsqlDbType.Text, endpoint.Url);
                cmd.Parameters.AddWithValue("method", NpgsqlDbType.Varchar, endpoint.Method);
                cmd.Parameters.AddWithValue("request_headers", NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(endpoint.RequestHeaders, _jsonOptions));
                cmd.Parameters.AddWithValue("request_body", NpgsqlDbType.Text,
                    (object?)endpoint.RequestBody ?? DBNull.Value);
                cmd.Parameters.AddWithValue("expected_status_code",
                    NpgsqlDbType.Integer, endpoint.ExpectedStatusCode);
                cmd.Parameters.AddWithValue("content_contains", NpgsqlDbType.Text,
                    (object?)endpoint.ContentContains ?? DBNull.Value);
                cmd.Parameters.AddWithValue("timeout_ms", NpgsqlDbType.Integer, endpoint.TimeoutMs);
                cmd.Parameters.AddWithValue("frequency_cron", NpgsqlDbType.Varchar, endpoint.FrequencyCron);
                cmd.Parameters.AddWithValue("is_active", NpgsqlDbType.Boolean, endpoint.IsActive);
                cmd.Parameters.AddWithValue("updated_by", NpgsqlDbType.Uuid, userId);

                var result = (bool?)await cmd.ExecuteScalarAsync(ct);
                return result ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: UpdateEndpointAsync {Id}", endpoint.Id);
                throw;
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task<bool> DeleteEndpointAsync(
            Guid id, Guid userId, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                const string sql = @"
                    UPDATE monitored_endpoints SET
                        is_deleted = true, deleted_at = NOW(), deleted_by = @DeletedBy
                    WHERE id = @Id AND is_deleted = false
                    RETURNING TRUE";

                var result = await connection.ExecuteScalarAsync<bool?>(
                    sql, new { Id = id, DeletedBy = userId });
                return result ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: DeleteEndpointAsync {Id}", id);
                throw;
            }
        }

        // ── UPDATE HANGFIRE JOB ID ─────────────────────────────────────────────
        public async Task UpdateHangfireJobIdAsync(
            Guid id, string jobId, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.ExecuteAsync(
                    "UPDATE monitored_endpoints SET hangfire_job_id=@JobId WHERE id=@Id",
                    new { Id = id, JobId = jobId });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to update HangfireJobId for endpoint {Id}", id);
            }
        }

        // ── INSERT LOG ────────────────────────────────────────────────────────
        public async Task InsertLogAsync(
            MonitoringLog log, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.ExecuteAsync(@"
                    INSERT INTO monitoring_logs
                        (endpoint_id, project_id, status, status_code,
                         response_time_ms, error_message, checked_at)
                    VALUES
                        (@EndpointId, @ProjectId, @Status, @StatusCode,
                         @ResponseTimeMs, @ErrorMessage, NOW())",
                    new
                    {
                        EndpointId = log.EndpointId,
                        ProjectId = log.ProjectId,
                        Status = (short)log.Status,
                        StatusCode = log.StatusCode,
                        ResponseTimeMs = log.ResponseTimeMs,
                        ErrorMessage = log.ErrorMessage
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: InsertLogAsync {EndpointId}", log.EndpointId);
                throw;
            }
        }

        // ── GET LOGS PAGED ────────────────────────────────────────────────────
        public async Task<(IEnumerable<MonitoringLog> Logs, int Total)> GetLogsPagedAsync(
            Guid endpointId, int page, int size, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                const string dataSql = @"
                    SELECT id, endpoint_id, project_id, status, status_code,
                           response_time_ms, error_message, checked_at
                    FROM monitoring_logs
                    WHERE endpoint_id = @EndpointId
                    ORDER BY checked_at DESC
                    LIMIT @Size OFFSET @Offset";

                const string countSql = @"
                    SELECT COUNT(*) FROM monitoring_logs
                    WHERE endpoint_id = @EndpointId";

                var logs = await connection.QueryAsync<MonitoringLogDb>(
                    dataSql, new
                    {
                        EndpointId = endpointId,
                        Size = size,
                        Offset = (page - 1) * size
                    });

                var total = await connection.ExecuteScalarAsync<int>(
                    countSql, new { EndpointId = endpointId });

                return (logs.Select(MapLogToEntity), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetLogsPagedAsync {EndpointId}", endpointId);
                throw;
            }
        }

        // ── GET LOGS IN RANGE ─────────────────────────────────────────────────
        public async Task<IEnumerable<MonitoringLog>> GetLogsInRangeAsync(
            Guid endpointId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                const string sql = @"
                    SELECT id, endpoint_id, project_id, status, status_code,
                           response_time_ms, error_message, checked_at
                    FROM monitoring_logs
                    WHERE endpoint_id = @EndpointId
                      AND checked_at BETWEEN @From AND @To
                    ORDER BY checked_at";

                var logs = await connection.QueryAsync<MonitoringLogDb>(
                    sql, new { EndpointId = endpointId, From = from, To = to });
                return logs.Select(MapLogToEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetLogsInRangeAsync {EndpointId}", endpointId);
                throw;
            }
        }

        // ── UPTIME STATS ──────────────────────────────────────────────────────
        public async Task<(double UptimePercent, double AvgResponseMs, int TotalChecks, int Failures)>
     GetUptimeStatsAsync(Guid endpointId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);

                const string sql = @"
            SELECT
                COUNT(*) AS TotalChecks,
                COUNT(*) FILTER (WHERE status = 2 OR status = 3) AS Failures,
                ROUND(AVG(response_time_ms) FILTER (WHERE response_time_ms IS NOT NULL), 2) AS AvgResponseMs
            FROM monitoring_logs
            WHERE endpoint_id = @EndpointId
              AND checked_at BETWEEN @From AND @To";

                var row = await connection.QueryFirstOrDefaultAsync<UptimeStatsDb>(
                    sql, new { EndpointId = endpointId, From = from, To = to });

                if (row is null)
                    return (100.0, 0, 0, 0);

                int total = row.TotalChecks;
                int failures = row.Failures;
                double avg = row.AvgResponseMs ?? 0;

                double uptime = total == 0
                    ? 100.0
                    : Math.Round((double)(total - failures) / total * 100, 2);

                return (uptime, avg, total, failures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetUptimeStatsAsync {EndpointId}", endpointId);
                return (0, 0, 0, 0);
            }
        }

        private class UptimeStatsDb
        {
            public int TotalChecks { get; set; }
            public int Failures { get; set; }
            public double? AvgResponseMs { get; set; }
        }
        // ── Private: DB models + mappers ──────────────────────────────────────
        private class MonitoredEndpointDb
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string Method { get; set; } = "GET";
            public string? RequestHeaders { get; set; }
            public string? RequestBody { get; set; }
            public int ExpectedStatusCode { get; set; }
            public string? ContentContains { get; set; }
            public int TimeoutMs { get; set; }
            public string FrequencyCron { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string? HangfireJobId { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid? UpdatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        private class MonitoringLogDb
        {
            public long Id { get; set; }
            public Guid EndpointId { get; set; }
            public Guid ProjectId { get; set; }
            public short Status { get; set; }
            public int? StatusCode { get; set; }
            public int? ResponseTimeMs { get; set; }
            public string? ErrorMessage { get; set; }
            public DateTime CheckedAt { get; set; }
        }

        private static MonitoredEndpoint MapToEntity(MonitoredEndpointDb db)
        {
            Dictionary<string, string> headers;
            try
            {
                headers = string.IsNullOrWhiteSpace(db.RequestHeaders)
                    ? new()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(
                        db.RequestHeaders, _jsonOptions) ?? new();
            }
            catch { headers = new(); }

            return new MonitoredEndpoint
            {
                Id = db.Id,
                ProjectId = db.ProjectId,
                Name = db.Name,
                Url = db.Url,
                Method = db.Method,
                RequestHeaders = headers,
                RequestBody = db.RequestBody,
                ExpectedStatusCode = db.ExpectedStatusCode,
                ContentContains = db.ContentContains,
                TimeoutMs = db.TimeoutMs,
                FrequencyCron = db.FrequencyCron,
                IsActive = db.IsActive,
                HangfireJobId = db.HangfireJobId,
                CreatedBy = db.CreatedBy,
                CreatedAt = db.CreatedAt,
                UpdatedBy = db.UpdatedBy,
                UpdatedAt = db.UpdatedAt,
                IsDeleted = db.IsDeleted
            };
        }

        private static MonitoringLog MapLogToEntity(MonitoringLogDb db) => new()
        {
            Id = db.Id,
            EndpointId = db.EndpointId,
            ProjectId = db.ProjectId,
            Status = (MonitorStatus)db.Status,
            StatusCode = db.StatusCode,
            ResponseTimeMs = db.ResponseTimeMs,
            ErrorMessage = db.ErrorMessage,
            CheckedAt = db.CheckedAt
        };
    }
}