using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using QAsist.Infrastructure.Persistence;
using System.Text.Json;

namespace QAsist.Infrastructure.Repository
{
    public class ExecutionRepository : IExecutionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<ExecutionRepository> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ExecutionRepository(
            IDbConnectionFactory connectionFactory,
            ILogger<ExecutionRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ── CREATE BATCH ──────────────────────────────────────────────────────
        public async Task<Guid> CreateBatchAsync(
            ExecutionBatch batch,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);
                var npgsql = (NpgsqlConnection)connection;

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO execution_batches
                        (id, test_suite_id, project_id, environment_id,
                         status, trigger, fail_fast, parallel_cases,
                         total_steps, passed_steps, failed_steps,
                         skipped_steps, error_steps,
                         started_at, created_by, created_at)
                    VALUES
                        (@id, @suite_id, @project_id, @env_id,
                         @status, @trigger, @fail_fast, @parallel,
                         0, 0, 0, 0, 0,
                         NOW(), @created_by, NOW())
                    RETURNING id";

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, batch.Id);
                cmd.Parameters.AddWithValue("suite_id", NpgsqlDbType.Uuid, batch.TestSuiteId);
                cmd.Parameters.AddWithValue("project_id", NpgsqlDbType.Uuid, batch.ProjectId);
                cmd.Parameters.AddWithValue("env_id", NpgsqlDbType.Uuid, batch.EnvironmentId);
                cmd.Parameters.AddWithValue("status", NpgsqlDbType.Smallint, (short)batch.Status);
                cmd.Parameters.AddWithValue("trigger", NpgsqlDbType.Smallint, (short)batch.Trigger);
                cmd.Parameters.AddWithValue("fail_fast", NpgsqlDbType.Boolean, batch.FailFast);
                cmd.Parameters.AddWithValue("parallel", NpgsqlDbType.Boolean, batch.ParallelCases);
                cmd.Parameters.AddWithValue("created_by", NpgsqlDbType.Uuid, batch.CreatedBy);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                _logger.LogDebug("Created execution batch {BatchId}", batch.Id);
                return (Guid)result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: CreateBatchAsync {BatchId}", batch.Id);
                throw;
            }
        }

        // ── UPDATE BATCH ──────────────────────────────────────────────────────
        public async Task<bool> UpdateBatchAsync(
            ExecutionBatch batch,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    UPDATE execution_batches SET
                        status        = @Status,
                        total_steps   = @Total,
                        passed_steps  = @Passed,
                        failed_steps  = @Failed,
                        skipped_steps = @Skipped,
                        error_steps   = @Error,
                        completed_at  = @CompletedAt,
                        updated_at    = NOW()
                    WHERE id = @Id
                    RETURNING TRUE";

                var result = await connection.ExecuteScalarAsync<bool?>(sql, new
                {
                    Id = batch.Id,
                    Status = (short)batch.Status,
                    Total = batch.TotalSteps,
                    Passed = batch.PassedSteps,
                    Failed = batch.FailedSteps,
                    Skipped = batch.SkippedSteps,
                    Error = batch.ErrorSteps,
                    CompletedAt = batch.CompletedAt
                });

                return result ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: UpdateBatchAsync {BatchId}", batch.Id);
                throw;
            }
        }

        // ── GET BATCH BY ID ───────────────────────────────────────────────────
        public async Task<ExecutionBatch?> GetBatchByIdAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    SELECT id, test_suite_id, project_id, environment_id,
                           status, trigger, fail_fast, parallel_cases,
                           total_steps, passed_steps, failed_steps,
                           skipped_steps, error_steps,
                           started_at, completed_at,
                           created_by, created_at, updated_at
                    FROM execution_batches
                    WHERE id = @Id AND is_deleted = false";

                var db = await connection.QueryFirstOrDefaultAsync<ExecutionBatchDb>(
                    sql, new { Id = batchId });

                return db is null ? null : MapBatchToEntity(db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetBatchByIdAsync {BatchId}", batchId);
                throw;
            }
        }

        // ── GET BATCH HISTORY ─────────────────────────────────────────────────
        public async Task<(IEnumerable<ExecutionBatch> Batches, int TotalCount)>
            GetBatchHistoryAsync(
                Guid suiteId, int pageNumber, int pageSize,
                CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string dataSql = @"
                    SELECT id, test_suite_id, project_id, environment_id,
                           status, trigger, fail_fast, parallel_cases,
                           total_steps, passed_steps, failed_steps,
                           skipped_steps, error_steps,
                           started_at, completed_at,
                           created_by, created_at, updated_at
                    FROM execution_batches
                    WHERE test_suite_id = @SuiteId AND is_deleted = false
                    ORDER BY created_at DESC
                    LIMIT @PageSize OFFSET @Offset";

                const string countSql = @"
                    SELECT COUNT(*) FROM execution_batches
                    WHERE test_suite_id = @SuiteId AND is_deleted = false";

                var batches = await connection.QueryAsync<ExecutionBatchDb>(
                    dataSql,
                    new
                    {
                        SuiteId = suiteId,
                        PageSize = pageSize,
                        Offset = (pageNumber - 1) * pageSize
                    });

                var total = await connection.ExecuteScalarAsync<int>(
                    countSql, new { SuiteId = suiteId });

                return (batches.Select(MapBatchToEntity), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetBatchHistoryAsync {SuiteId}", suiteId);
                throw;
            }
        }

        // ── SAVE STEP RESULT ──────────────────────────────────────────────────
        public async Task SaveStepResultAsync(
            ExecutionResult result,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);
                var npgsql = (NpgsqlConnection)connection;

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO execution_results
                        (id, batch_id, test_suite_id, test_case_id, test_step_id,
                         status, duration_ms, error_message,
                         request_log, response_log,
                         assertion_results, extracted_variables,
                         executed_at, created_by, created_at)
                    VALUES
                        (@id, @batch_id, @suite_id, @case_id, @step_id,
                         @status, @duration_ms, @error_message,
                         @request_log, @response_log,
                         @assertion_results, @extracted_variables,
                         NOW(), @created_by, NOW())";

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, result.Id);
                cmd.Parameters.AddWithValue("batch_id", NpgsqlDbType.Uuid, result.BatchId);
                cmd.Parameters.AddWithValue("suite_id", NpgsqlDbType.Uuid, result.TestSuiteId);
                cmd.Parameters.AddWithValue("case_id", NpgsqlDbType.Uuid, result.TestCaseId);
                cmd.Parameters.AddWithValue("step_id", NpgsqlDbType.Uuid, result.TestStepId);
                cmd.Parameters.AddWithValue("status", NpgsqlDbType.Smallint, (short)result.Status);
                cmd.Parameters.AddWithValue("duration_ms", NpgsqlDbType.Integer, (int)result.DurationMs);
                cmd.Parameters.AddWithValue("created_by", NpgsqlDbType.Uuid, result.CreatedBy);

                // Nullable text
                cmd.Parameters.AddWithValue("error_message",
                    NpgsqlDbType.Text,
                    (object?)result.ErrorMessage ?? DBNull.Value);

                // JSONB logs
                cmd.Parameters.AddWithValue("request_log",
                    NpgsqlDbType.Jsonb,
                    result.RequestLog is null
                        ? DBNull.Value
                        : JsonSerializer.Serialize(result.RequestLog, _jsonOptions));

                cmd.Parameters.AddWithValue("response_log",
                    NpgsqlDbType.Jsonb,
                    result.ResponseLog is null
                        ? DBNull.Value
                        : JsonSerializer.Serialize(result.ResponseLog, _jsonOptions));

                cmd.Parameters.AddWithValue("assertion_results",
                    NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(result.AssertionResults, _jsonOptions));

                cmd.Parameters.AddWithValue("extracted_variables",
                    NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(result.ExtractedVariables, _jsonOptions));

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogDebug(
                    "Saved execution result {ResultId} status={Status}",
                    result.Id, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: SaveStepResultAsync {ResultId}", result.Id);
                throw;
            }
        }

        // ── GET RESULTS BY BATCH ──────────────────────────────────────────────
        public async Task<IEnumerable<ExecutionResult>> GetResultsByBatchAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    SELECT id, batch_id, test_suite_id, test_case_id, test_step_id,
                           status, duration_ms, error_message,
                           request_log, response_log,
                           assertion_results, extracted_variables,
                           executed_at, created_by, created_at
                    FROM execution_results
                    WHERE batch_id = @BatchId
                    ORDER BY executed_at";

                var results = await connection.QueryAsync<ExecutionResultDb>(
                    sql, new { BatchId = batchId });

                return results.Select(MapResultToEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetResultsByBatchAsync {BatchId}", batchId);
                throw;
            }
        }

        // ── GET RESULTS PAGED ─────────────────────────────────────────────────
        public async Task<(IEnumerable<ExecutionResult> Results, int TotalCount)>
            GetResultsByBatchPagedAsync(
                Guid batchId, int pageNumber, int pageSize,
                CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string dataSql = @"
                    SELECT id, batch_id, test_suite_id, test_case_id, test_step_id,
                           status, duration_ms, error_message,
                           request_log, response_log,
                           assertion_results, extracted_variables,
                           executed_at, created_by, created_at
                    FROM execution_results
                    WHERE batch_id = @BatchId
                    ORDER BY executed_at
                    LIMIT @PageSize OFFSET @Offset";

                const string countSql = @"
                    SELECT COUNT(*) FROM execution_results WHERE batch_id = @BatchId";

                var results = await connection.QueryAsync<ExecutionResultDb>(
                    dataSql,
                    new
                    {
                        BatchId = batchId,
                        PageSize = pageSize,
                        Offset = (pageNumber - 1) * pageSize
                    });

                var total = await connection.ExecuteScalarAsync<int>(
                    countSql, new { BatchId = batchId });

                return (results.Select(MapResultToEntity), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetResultsByBatchPagedAsync {BatchId}", batchId);
                throw;
            }
        }

        // ── PRIVATE: DB Models ────────────────────────────────────────────────
        private class ExecutionBatchDb
        {
            public Guid Id { get; set; }
            public Guid TestSuiteId { get; set; }
            public Guid ProjectId { get; set; }
            public Guid EnvironmentId { get; set; }
            public short Status { get; set; }
            public short Trigger { get; set; }
            public bool FailFast { get; set; }
            public bool ParallelCases { get; set; }
            public int TotalSteps { get; set; }
            public int PassedSteps { get; set; }
            public int FailedSteps { get; set; }
            public int SkippedSteps { get; set; }
            public int ErrorSteps { get; set; }
            public DateTime? StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        private class ExecutionResultDb
        {
            public Guid Id { get; set; }
            public Guid BatchId { get; set; }
            public Guid TestSuiteId { get; set; }
            public Guid TestCaseId { get; set; }
            public Guid TestStepId { get; set; }
            public short Status { get; set; }
            public int DurationMs { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RequestLog { get; set; }
            public string? ResponseLog { get; set; }
            public string? AssertionResults { get; set; }
            public string? ExtractedVariables { get; set; }
            public DateTime ExecutedAt { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // ── PRIVATE: Mappers ──────────────────────────────────────────────────
        private static ExecutionBatch MapBatchToEntity(ExecutionBatchDb db) => new()
        {
            Id = db.Id,
            TestSuiteId = db.TestSuiteId,
            ProjectId = db.ProjectId,
            EnvironmentId = db.EnvironmentId,
            Status = (ExecutionStatus)db.Status,
            Trigger = (ExecutionTrigger)db.Trigger,
            FailFast = db.FailFast,
            ParallelCases = db.ParallelCases,
            TotalSteps = db.TotalSteps,
            PassedSteps = db.PassedSteps,
            FailedSteps = db.FailedSteps,
            SkippedSteps = db.SkippedSteps,
            ErrorSteps = db.ErrorSteps,
            StartedAt = db.StartedAt,
            CompletedAt = db.CompletedAt,
            CreatedBy = db.CreatedBy,
            CreatedAt = db.CreatedAt,
            UpdatedAt = db.UpdatedAt
        };

        private static ExecutionResult MapResultToEntity(ExecutionResultDb db)
        {
            List<AssertionResult> assertionResults;
            try
            {
                assertionResults = string.IsNullOrWhiteSpace(db.AssertionResults)
                    ? new()
                    : JsonSerializer.Deserialize<List<AssertionResult>>(
                        db.AssertionResults, _jsonOptions) ?? new();
            }
            catch { assertionResults = new(); }

            Dictionary<string, string> extractedVars;
            try
            {
                extractedVars = string.IsNullOrWhiteSpace(db.ExtractedVariables)
                    ? new()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(
                        db.ExtractedVariables, _jsonOptions) ?? new();
            }
            catch { extractedVars = new(); }

            return new ExecutionResult
            {
                Id = db.Id,
                BatchId = db.BatchId,
                TestSuiteId = db.TestSuiteId,
                TestCaseId = db.TestCaseId,
                TestStepId = db.TestStepId,
                Status = (StepStatus)db.Status,
                DurationMs = db.DurationMs,
                ErrorMessage = db.ErrorMessage,
                AssertionResults = assertionResults,
                ExtractedVariables = extractedVars,
                ExecutedAt = db.ExecutedAt,
                CreatedBy = db.CreatedBy,
                CreatedAt = db.CreatedAt
            };
        }
    }
}
