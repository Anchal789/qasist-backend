//using Dapper;
//using Microsoft.Extensions.Logging;
//using Npgsql;
//using NpgsqlTypes;
//using QAsist.Application.DTOs;
//using QAsist.Application.Interfaces.IRepositories;
//using QAsist.Domain.Entities;
//using QAsist.Domain.Enums;
//using QAsist.Infrastructure.Persistence;
//using System.Text.Json;

//namespace QAsist.Infrastructure.Repository
//{
//    public class TestCaseRepository : ITestCaseRepository
//    {
//        private readonly IDbConnectionFactory _connectionFactory;
//        private readonly ILogger<TestCaseRepository> _logger;

//        private static readonly JsonSerializerOptions _jsonOptions = new()
//        {
//            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//        };

//        public TestCaseRepository(
//            IDbConnectionFactory connectionFactory,
//            ILogger<TestCaseRepository> logger)
//        {
//            _connectionFactory = connectionFactory;
//            _logger = logger;
//        }

//        // ── GET BY ID ─────────────────────────────────────────────────────────
//        public async Task<TestCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var result = await connection.QueryFirstOrDefaultAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetById,
//                    new { p_id = id });
//                return result != null ? MapToTestCase(result) : null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetByIdAsync {TestCaseId}", id);
//                throw;
//            }
//        }

//        // ── GET BY PROJECT ────────────────────────────────────────────────────
//        public async Task<IEnumerable<TestCase>> GetByProjectAsync(
//            Guid projectId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var results = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetByProject, new { p_project_id = projectId });
//                return results.Select(MapToTestCase);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetByProjectAsync {ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── GET SUMMARY BY PROJECT ────────────────────────────────────────────
//        public async Task<IEnumerable<TestCase>> GetSummaryByProjectAsync(
//            Guid projectId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var results = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetByProject, new { p_project_id = projectId });
//                return results.Select(MapToTestCase);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetSummaryByProjectAsync {ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── GET PAGED ─────────────────────────────────────────────────────────
//        public async Task<(IEnumerable<TestCase> TestCases, int TotalCount)> GetPagedAsync(
//            Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var testCases = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetPaged,
//                    new { p_project_id = projectId, p_page_number = pageNumber, p_page_size = pageSize });
//                var totalCount = await connection.ExecuteScalarAsync<int>(
//                    SqlQueries.TestCases.GetCount, new { p_project_id = projectId });
//                return (testCases.Select(MapToTestCase), totalCount);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetPagedAsync {ProjectId} page={Page}", projectId, pageNumber);
//                throw;
//            }
//        }

//        // ── GET BY STATUS ─────────────────────────────────────────────────────
//        public async Task<IEnumerable<TestCase>> GetByStatusAsync(
//            Guid projectId, TestCaseStatus status, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var results = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetByStatus,
//                    new { p_project_id = projectId, p_status = (int)status });
//                return results.Select(MapToTestCase);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetByStatusAsync {ProjectId} status={Status}", projectId, status);
//                throw;
//            }
//        }

//        // ── GET AI GENERATED ──────────────────────────────────────────────────
//        public async Task<IEnumerable<TestCase>> GetAiGeneratedAsync(
//            Guid projectId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var results = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetAiGenerated, new { p_project_id = projectId });
//                return results.Select(MapToTestCase);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetAiGeneratedAsync {ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── GET BY ASSIGNEE ───────────────────────────────────────────────────
//        public async Task<IEnumerable<TestCase>> GetByAssigneeAsync(
//            Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var results = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.GetByAssignee, new { p_user_id = userId });
//                return results.Select(MapToTestCase);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetByAssigneeAsync {UserId}", userId);
//                throw;
//            }
//        }

//        // ── CREATE ────────────────────────────────────────────────────────────
//        // FIX: p_steps is JSONB in your function — must use NpgsqlDbType.Jsonb
//        // Dapper anonymous objects send everything as TEXT, causing
//        // "function does not exist" because PostgreSQL finds no matching overload
//        public async Task<Guid> CreateAsync(
//            TestCase testCase, Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var npgsql = (NpgsqlConnection)connection;

//                var stepsJson = JsonSerializer.Serialize(testCase.Steps, _jsonOptions);

//                await using var cmd = npgsql.CreateCommand();
//                cmd.CommandText = SqlQueries.TestCases.Create;

//                cmd.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, testCase.Id);
//                cmd.Parameters.AddWithValue("p_project_id", NpgsqlDbType.Uuid, testCase.ProjectId);
//                cmd.Parameters.AddWithValue("p_endpoint", NpgsqlDbType.Varchar, testCase.Endpoint);
//                cmd.Parameters.AddWithValue("p_method", NpgsqlDbType.Varchar, testCase.Method);
//                cmd.Parameters.AddWithValue("p_title", NpgsqlDbType.Varchar, testCase.Title);
//                cmd.Parameters.AddWithValue("p_steps", NpgsqlDbType.Jsonb, stepsJson);
//                cmd.Parameters.AddWithValue("p_expected_result", NpgsqlDbType.Text, testCase.ExpectedResult);
//                cmd.Parameters.AddWithValue("p_priority", NpgsqlDbType.Integer, (int)testCase.Priority);
//                cmd.Parameters.AddWithValue("p_status", NpgsqlDbType.Integer, (int)testCase.Status);
//                cmd.Parameters.AddWithValue("p_is_ai_generated", NpgsqlDbType.Boolean, testCase.IsAiGenerated);
//                cmd.Parameters.AddWithValue("p_created_by", NpgsqlDbType.Uuid, userId);
//                AddNullableUuid(cmd, "p_assigned_to", testCase.AssignedTo);

//                var result = await cmd.ExecuteScalarAsync(cancellationToken);
//                var createdId = (Guid)result!;

//                _logger.LogDebug("Created test case {TestCaseId} in DB", createdId);
//                return createdId;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: CreateAsync project={ProjectId}", testCase.ProjectId);
//                throw;
//            }
//        }

//        // ── BULK CREATE (existing — unchanged, AI uses its own JSON path) ────
//        public async Task<int> BulkCreateAsync(
//            List<GeneratedTestCaseDto> testCases, Guid projectId, Guid userId,
//            CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.ExecuteScalarAsync<int>(
//                    SqlQueries.TestCases.BulkCreate,
//                    new
//                    {
//                        p_test_cases = JsonSerializer.Serialize(testCases, _jsonOptions),
//                        p_project_id = projectId,
//                        p_created_by = userId
//                    });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: BulkCreateAsync project={ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── UPDATE ────────────────────────────────────────────────────────────
//        // FIX: same JSONB issue — p_steps must be NpgsqlDbType.Jsonb
//        public async Task<bool> UpdateAsync(
//            TestCase testCase, Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var npgsql = (NpgsqlConnection)connection;

//                var stepsJson = JsonSerializer.Serialize(testCase.Steps, _jsonOptions);

//                await using var cmd = npgsql.CreateCommand();
//                cmd.CommandText = SqlQueries.TestCases.Update;

//                cmd.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, testCase.Id);
//                cmd.Parameters.AddWithValue("p_endpoint", NpgsqlDbType.Varchar, testCase.Endpoint);
//                cmd.Parameters.AddWithValue("p_method", NpgsqlDbType.Varchar, testCase.Method);
//                cmd.Parameters.AddWithValue("p_title", NpgsqlDbType.Varchar, testCase.Title);
//                cmd.Parameters.AddWithValue("p_steps", NpgsqlDbType.Jsonb, stepsJson);
//                cmd.Parameters.AddWithValue("p_expected_result", NpgsqlDbType.Text, testCase.ExpectedResult);
//                cmd.Parameters.AddWithValue("p_priority", NpgsqlDbType.Integer, (int)testCase.Priority);
//                cmd.Parameters.AddWithValue("p_status", NpgsqlDbType.Integer, (int)testCase.Status);
//                cmd.Parameters.AddWithValue("p_updated_by", NpgsqlDbType.Uuid, userId);
//                AddNullableUuid(cmd, "p_assigned_to", testCase.AssignedTo);
//                AddNullableJsonb(cmd, "p_request_headers", testCase.RequestHeaders);
//                AddNullableText(cmd, "p_request_body", testCase.RequestBody);
//                AddNullableInteger(cmd, "p_expected_status_code", testCase.ExpectedStatusCode);
//                AddNullableText(cmd, "p_expected_body_contains", testCase.ExpectedBodyContains);
//                AddNullableInteger(cmd, "p_expected_response_time_ms", testCase.ExpectedResponseTimeMs);

//                var result = await cmd.ExecuteScalarAsync(cancellationToken);
//                var updated = result is bool b ? b : result is not null;

//                _logger.LogDebug("Updated test case {TestCaseId} in DB", testCase.Id);
//                return updated;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: UpdateAsync {TestCaseId}", testCase.Id);
//                throw;
//            }
//        }

//        // ── UPDATE STATUS ─────────────────────────────────────────────────────
//        public async Task<bool> UpdateStatusAsync(
//            Guid id, TestCaseStatus status, Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.ExecuteScalarAsync<bool>(
//                    SqlQueries.TestCases.UpdateStatus,
//                    new { p_id = id, p_status = (int)status, p_updated_by = userId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: UpdateStatusAsync {TestCaseId}", id);
//                throw;
//            }
//        }

//        // ── ASSIGN ────────────────────────────────────────────────────────────
//        public async Task<bool> AssignAsync(
//            Guid id, Guid assignedTo, Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.ExecuteScalarAsync<bool>(
//                    SqlQueries.TestCases.Assign,
//                    new { p_id = id, p_assigned_to = assignedTo, p_updated_by = userId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: AssignAsync {TestCaseId}", id);
//                throw;
//            }
//        }

//        // ── DELETE (soft) ─────────────────────────────────────────────────────
//        public async Task<bool> DeleteAsync(
//            Guid id, Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.ExecuteScalarAsync<bool>(
//                    SqlQueries.TestCases.Delete,
//                    new { p_id = id, p_deleted_by = userId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: DeleteAsync {TestCaseId}", id);
//                throw;
//            }
//        }

//        // ── BULK DELETE ───────────────────────────────────────────────────────
//        public async Task<int> BulkDeleteByProjectAsync(
//            Guid projectId, Guid userId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.ExecuteScalarAsync<int>(
//                    SqlQueries.TestCases.BulkDeleteByProject,
//                    new { p_project_id = projectId, p_deleted_by = userId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: BulkDeleteByProjectAsync {ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── STATISTICS ────────────────────────────────────────────────────────
//        public async Task<TestCaseStatisticsDto> GetStatisticsAsync(
//            Guid projectId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.QueryFirstOrDefaultAsync<TestCaseStatisticsDto>(
//                    SqlQueries.TestCases.GetStatistics,
//                    new { p_project_id = projectId }) ?? new TestCaseStatisticsDto();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetStatisticsAsync {ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── ENDPOINT COVERAGE ─────────────────────────────────────────────────
//        public async Task<IEnumerable<EndpointCoverageDto>> GetEndpointCoverageAsync(
//            Guid projectId, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.QueryAsync<EndpointCoverageDto>(
//                    SqlQueries.TestCases.GetEndpointCoverage,
//                    new { p_project_id = projectId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: GetEndpointCoverageAsync {ProjectId}", projectId);
//                throw;
//            }
//        }

//        // ── SEARCH ────────────────────────────────────────────────────────────
//        public async Task<IEnumerable<TestCase>> SearchAsync(
//            Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                var results = await connection.QueryAsync<TestCaseDb>(
//                    SqlQueries.TestCases.Search,
//                    new { p_project_id = projectId, p_search_term = searchTerm });
//                return results.Select(MapToTestCase);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: SearchAsync {ProjectId} term='{Term}'", projectId, searchTerm);
//                throw;
//            }
//        }

//        // ── EXISTS ────────────────────────────────────────────────────────────
//        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
//                return await connection.ExecuteScalarAsync<bool>(
//                    SqlQueries.TestCases.Exists,
//                    new { p_id = id });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Repository error: ExistsAsync {TestCaseId}", id);
//                throw;
//            }
//        }

//        // ── PRIVATE: NULLABLE PARAMETER HELPERS ──────────────────────────────
//        private static void AddNullableUuid(NpgsqlCommand cmd, string name, Guid? value)
//        {
//            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Uuid,
//                value.HasValue ? (object)value.Value : DBNull.Value);
//        }

//        private static void AddNullableJsonb(NpgsqlCommand cmd, string name, string? value)
//        {
//            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Jsonb,
//                value is not null ? (object)value : DBNull.Value);
//        }

//        private static void AddNullableText(NpgsqlCommand cmd, string name, string? value)
//        {
//            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Text,
//                value is not null ? (object)value : DBNull.Value);
//        }

//        private static void AddNullableInteger(NpgsqlCommand cmd, string name, int? value)
//        {
//            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Integer,
//                value.HasValue ? (object)value.Value : DBNull.Value);
//        }

//        // ── PRIVATE: DB MODEL ─────────────────────────────────────────────────
//        private class TestCaseDb
//        {
//            public Guid Id { get; set; }
//            public Guid ProjectId { get; set; }
//            public string Endpoint { get; set; } = string.Empty;
//            public string Method { get; set; } = string.Empty;
//            public string Title { get; set; } = string.Empty;
//            public string? Steps { get; set; }               // JSONB → string from Dapper
//            public string ExpectedResult { get; set; } = string.Empty;
//            public int Priority { get; set; }
//            public int Status { get; set; }
//            public Guid? AssignedTo { get; set; }
//            public bool IsAiGenerated { get; set; }
//            public DateTime CreatedAt { get; set; }
//            public DateTime? UpdatedAt { get; set; }
//            public Guid CreatedBy { get; set; }
//            public Guid? UpdatedBy { get; set; }
//            public bool IsDeleted { get; set; }
//            public DateTime? DeletedAt { get; set; }
//            public Guid? DeletedBy { get; set; }
//            // New optional columns added by V003 migration
//            public string? RequestHeaders { get; set; }
//            public string? RequestBody { get; set; }
//            public int? ExpectedStatusCode { get; set; }
//            public string? ExpectedBodyContains { get; set; }
//            public int? ExpectedResponseTimeMs { get; set; }
//        }

//        // ── PRIVATE: MAPPER ───────────────────────────────────────────────────
//        private static TestCase MapToTestCase(TestCaseDb db)
//        {
//            List<string> steps;
//            try
//            {
//                steps = string.IsNullOrWhiteSpace(db.Steps)
//                    ? new List<string>()
//                    : JsonSerializer.Deserialize<List<string>>(db.Steps, _jsonOptions)
//                      ?? new List<string>();
//            }
//            catch
//            {
//                steps = new List<string>();
//            }

//            return new TestCase
//            {
//                Id = db.Id,
//                ProjectId = db.ProjectId,
//                Endpoint = db.Endpoint,
//                Method = db.Method,
//                Title = db.Title,
//                Steps = steps,
//                ExpectedResult = db.ExpectedResult,
//                Priority = (TestCasePriority)db.Priority,
//                Status = (TestCaseStatus)db.Status,
//                AssignedTo = db.AssignedTo,
//                IsAiGenerated = db.IsAiGenerated,
//                CreatedAt = db.CreatedAt,
//                UpdatedAt = db.UpdatedAt,
//                CreatedBy = db.CreatedBy,
//                UpdatedBy = db.UpdatedBy,
//                IsDeleted = db.IsDeleted,
//                DeletedAt = db.DeletedAt,
//                DeletedBy = db.DeletedBy,
//                RequestHeaders = db.RequestHeaders,
//                RequestBody = db.RequestBody,
//                ExpectedStatusCode = db.ExpectedStatusCode,
//                ExpectedBodyContains = db.ExpectedBodyContains,
//                ExpectedResponseTimeMs = db.ExpectedResponseTimeMs
//            };
//        }
//    }
//}

using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Infrastructure.Persistence;
using System.Text.Json;

namespace QAsist.Infrastructure.Repository
{
    public class TestCaseRepository : ITestCaseRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<TestCaseRepository> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public TestCaseRepository(
            IDbConnectionFactory connectionFactory,
            ILogger<TestCaseRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<TestCase?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — Id={Id}", nameof(GetByIdAsync), id);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var result = await connection.QueryFirstOrDefaultAsync<TestCaseDb>(
                    SqlQueries.TestCases.GetById, new { p_id = id });

                _logger.LogInformation("Completed {Method} — Found={Found}", nameof(GetByIdAsync), result != null);
                return result != null ? MapToTestCase(result) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method} for Id={Id}", nameof(GetByIdAsync), id);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {Method}", nameof(GetByIdAsync));
            }
        }

        // ── GET BY PROJECT ────────────────────────────────────────────────────
        public async Task<IEnumerable<TestCase>> GetByProjectAsync(
            Guid projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — ProjectId={ProjectId}", nameof(GetByProjectAsync), projectId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var results = await connection.QueryAsync<TestCaseDb>(
                    SqlQueries.TestCases.GetByProject, new { p_project_id = projectId });
                var mapped = results.Select(MapToTestCase).ToList();

                _logger.LogInformation("Completed {Method} — Count={Count}", nameof(GetByProjectAsync), mapped.Count);
                return mapped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method} for ProjectId={ProjectId}", nameof(GetByProjectAsync), projectId);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {Method}", nameof(GetByProjectAsync));
            }
        }

        // ── GET PAGED ─────────────────────────────────────────────────────────
        public async Task<(IEnumerable<TestCase> TestCases, int TotalCount)> GetPagedAsync(
            Guid projectId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — ProjectId={ProjectId}, Page={Page}",
                nameof(GetPagedAsync), projectId, pageNumber);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var testCases = await connection.QueryAsync<TestCaseDb>(
                    SqlQueries.TestCases.GetPaged,
                    new { p_project_id = projectId, p_page_number = pageNumber, p_page_size = pageSize });
                var totalCount = await connection.ExecuteScalarAsync<int>(
                    SqlQueries.TestCases.GetCount, new { p_project_id = projectId });

                _logger.LogInformation("Completed {Method} — Total={Total}", nameof(GetPagedAsync), totalCount);
                return (testCases.Select(MapToTestCase), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetPagedAsync));
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {Method}", nameof(GetPagedAsync));
            }
        }

        // ── GET BY STATUS ─────────────────────────────────────────────────────
        public async Task<IEnumerable<TestCase>> GetByStatusAsync(
            Guid projectId, TestCaseStatus status,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(GetByStatusAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var results = await connection.QueryAsync<TestCaseDb>(
                    SqlQueries.TestCases.GetByStatus,
                    new { p_project_id = projectId, p_status = (int)status });

                _logger.LogInformation("Completed {Method}", nameof(GetByStatusAsync));
                return results.Select(MapToTestCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetByStatusAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(GetByStatusAsync)); }
        }

        // ── GET AI GENERATED ──────────────────────────────────────────────────
        public async Task<IEnumerable<TestCase>> GetAiGeneratedAsync(
            Guid projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(GetAiGeneratedAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var results = await connection.QueryAsync<TestCaseDb>(
                    SqlQueries.TestCases.GetAiGenerated, new { p_project_id = projectId });

                _logger.LogInformation("Completed {Method}", nameof(GetAiGeneratedAsync));
                return results.Select(MapToTestCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetAiGeneratedAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(GetAiGeneratedAsync)); }
        }

        // ── GET BY ASSIGNEE ───────────────────────────────────────────────────
        public async Task<IEnumerable<TestCase>> GetByAssigneeAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(GetByAssigneeAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var results = await connection.QueryAsync<TestCaseDb>(
                    SqlQueries.TestCases.GetByAssignee, new { p_user_id = userId });

                _logger.LogInformation("Completed {Method}", nameof(GetByAssigneeAsync));
                return results.Select(MapToTestCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetByAssigneeAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(GetByAssigneeAsync)); }
        }

        // ── GET SUMMARY BY PROJECT ────────────────────────────────────────────
        public async Task<IEnumerable<TestCase>> GetSummaryByProjectAsync(
            Guid projectId, CancellationToken cancellationToken = default)
            => await GetByProjectAsync(projectId, cancellationToken);

        // ── CREATE ────────────────────────────────────────────────────────────
        // FIX: Steps is now List<ExecutableStep>, serialised as JSONB array of objects
        public async Task<Guid> CreateAsync(
            TestCase testCase, Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — ProjectId={ProjectId}, Title={Title}",
                nameof(CreateAsync), testCase.ProjectId, testCase.Title);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var npgsql = (NpgsqlConnection)connection;

                // FIX: serialize ExecutableStep objects, NOT List<string>
                var stepsJson = JsonSerializer.Serialize(testCase.Steps, _jsonOptions);

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = SqlQueries.TestCases.Create;

                cmd.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, testCase.Id);
                cmd.Parameters.AddWithValue("p_project_id", NpgsqlDbType.Uuid, testCase.ProjectId);
                cmd.Parameters.AddWithValue("p_endpoint", NpgsqlDbType.Varchar, testCase.Endpoint);
                cmd.Parameters.AddWithValue("p_method", NpgsqlDbType.Varchar, testCase.Method);
                cmd.Parameters.AddWithValue("p_title", NpgsqlDbType.Varchar, testCase.Title);
                cmd.Parameters.AddWithValue("p_steps", NpgsqlDbType.Jsonb, stepsJson);
                cmd.Parameters.AddWithValue("p_expected_result", NpgsqlDbType.Text, testCase.ExpectedResult);
                cmd.Parameters.AddWithValue("p_priority", NpgsqlDbType.Integer, (int)testCase.Priority);
                cmd.Parameters.AddWithValue("p_status", NpgsqlDbType.Integer, (int)testCase.Status);
                cmd.Parameters.AddWithValue("p_is_ai_generated", NpgsqlDbType.Boolean, testCase.IsAiGenerated);
                cmd.Parameters.AddWithValue("p_created_by", NpgsqlDbType.Uuid, userId);
                AddNullableUuid(cmd, "p_assigned_to", testCase.AssignedTo);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var createdId = (Guid)result!;

                _logger.LogInformation("Completed {Method} — CreatedId={Id}", nameof(CreateAsync), createdId);
                return createdId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(CreateAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(CreateAsync)); }
        }

        // ── BULK CREATE ───────────────────────────────────────────────────────
        public async Task<int> BulkCreateAsync(
            List<GeneratedTestCaseDto> testCases, Guid projectId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — Count={Count}", nameof(BulkCreateAsync), testCases.Count);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var count = await connection.ExecuteScalarAsync<int>(
                    SqlQueries.TestCases.BulkCreate,
                    new
                    {
                        p_test_cases = JsonSerializer.Serialize(testCases, _jsonOptions),
                        p_project_id = projectId,
                        p_created_by = userId
                    });

                _logger.LogInformation("Completed {Method} — Inserted={Count}", nameof(BulkCreateAsync), count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(BulkCreateAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(BulkCreateAsync)); }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<bool> UpdateAsync(
            TestCase testCase, Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — Id={Id}", nameof(UpdateAsync), testCase.Id);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var npgsql = (NpgsqlConnection)connection;

                var stepsJson = JsonSerializer.Serialize(testCase.Steps, _jsonOptions);

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = SqlQueries.TestCases.Update;

                cmd.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, testCase.Id);
                cmd.Parameters.AddWithValue("p_endpoint", NpgsqlDbType.Varchar, testCase.Endpoint);
                cmd.Parameters.AddWithValue("p_method", NpgsqlDbType.Varchar, testCase.Method);
                cmd.Parameters.AddWithValue("p_title", NpgsqlDbType.Varchar, testCase.Title);
                cmd.Parameters.AddWithValue("p_steps", NpgsqlDbType.Jsonb, stepsJson);
                cmd.Parameters.AddWithValue("p_expected_result", NpgsqlDbType.Text, testCase.ExpectedResult);
                cmd.Parameters.AddWithValue("p_priority", NpgsqlDbType.Integer, (int)testCase.Priority);
                cmd.Parameters.AddWithValue("p_status", NpgsqlDbType.Integer, (int)testCase.Status);
                cmd.Parameters.AddWithValue("p_updated_by", NpgsqlDbType.Uuid, userId);
                AddNullableUuid(cmd, "p_assigned_to", testCase.AssignedTo);
                AddNullableJsonb(cmd, "p_request_headers", testCase.RequestHeaders);
                AddNullableText(cmd, "p_request_body", testCase.RequestBody);
                AddNullableInteger(cmd, "p_expected_status_code", testCase.ExpectedStatusCode);
                AddNullableText(cmd, "p_expected_body_contains", testCase.ExpectedBodyContains);
                AddNullableInteger(cmd, "p_expected_response_time_ms", testCase.ExpectedResponseTimeMs);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var updated = result is bool b ? b : result is not null;

                _logger.LogInformation("Completed {Method} — Updated={Updated}", nameof(UpdateAsync), updated);
                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(UpdateAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(UpdateAsync)); }
        }

        // ── UPDATE STATUS ─────────────────────────────────────────────────────
        public async Task<bool> UpdateStatusAsync(
            Guid id, TestCaseStatus status, Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(UpdateStatusAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var result = await connection.ExecuteScalarAsync<bool>(
                    SqlQueries.TestCases.UpdateStatus,
                    new { p_id = id, p_status = (int)status, p_updated_by = userId });

                _logger.LogInformation("Completed {Method}", nameof(UpdateStatusAsync));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(UpdateStatusAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(UpdateStatusAsync)); }
        }

        // ── ASSIGN ────────────────────────────────────────────────────────────
        public async Task<bool> AssignAsync(
            Guid id, Guid assignedTo, Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(AssignAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var result = await connection.ExecuteScalarAsync<bool>(
                    SqlQueries.TestCases.Assign,
                    new { p_id = id, p_assigned_to = assignedTo, p_updated_by = userId });

                _logger.LogInformation("Completed {Method}", nameof(AssignAsync));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(AssignAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(AssignAsync)); }
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(
            Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(DeleteAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var result = await connection.ExecuteScalarAsync<bool>(
                    SqlQueries.TestCases.Delete,
                    new { p_id = id, p_deleted_by = userId });

                _logger.LogInformation("Completed {Method}", nameof(DeleteAsync));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(DeleteAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(DeleteAsync)); }
        }

        // ── BULK DELETE ───────────────────────────────────────────────────────
        public async Task<int> BulkDeleteByProjectAsync(
            Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(BulkDeleteByProjectAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var count = await connection.ExecuteScalarAsync<int>(
                    SqlQueries.TestCases.BulkDeleteByProject,
                    new { p_project_id = projectId, p_deleted_by = userId });

                _logger.LogInformation("Completed {Method} — Deleted={Count}", nameof(BulkDeleteByProjectAsync), count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(BulkDeleteByProjectAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(BulkDeleteByProjectAsync)); }
        }

        // ── STATISTICS ────────────────────────────────────────────────────────
        public async Task<TestCaseStatisticsDto> GetStatisticsAsync(
            Guid projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(GetStatisticsAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var result = await connection.QueryFirstOrDefaultAsync<TestCaseStatisticsDto>(
                    SqlQueries.TestCases.GetStatistics,
                    new { p_project_id = projectId }) ?? new TestCaseStatisticsDto();

                _logger.LogInformation("Completed {Method}", nameof(GetStatisticsAsync));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetStatisticsAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(GetStatisticsAsync)); }
        }

        // ── ENDPOINT COVERAGE ─────────────────────────────────────────────────
        public async Task<IEnumerable<EndpointCoverageDto>> GetEndpointCoverageAsync(
            Guid projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method}", nameof(GetEndpointCoverageAsync));
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var result = await connection.QueryAsync<EndpointCoverageDto>(
                    SqlQueries.TestCases.GetEndpointCoverage,
                    new { p_project_id = projectId });

                _logger.LogInformation("Completed {Method}", nameof(GetEndpointCoverageAsync));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetEndpointCoverageAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(GetEndpointCoverageAsync)); }
        }

        // ── SEARCH ────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TestCase>> SearchAsync(
            Guid projectId, string searchTerm,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {Method} — Term='{Term}'", nameof(SearchAsync), searchTerm);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                var results = await connection.QueryAsync<TestCaseDb>(
                    SqlQueries.TestCases.Search,
                    new { p_project_id = projectId, p_search_term = searchTerm });

                _logger.LogInformation("Completed {Method}", nameof(SearchAsync));
                return results.Select(MapToTestCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(SearchAsync));
                throw;
            }
            finally { _logger.LogInformation("Finished {Method}", nameof(SearchAsync)); }
        }

        // ── EXISTS ────────────────────────────────────────────────────────────
        public async Task<bool> ExistsAsync(
            Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                return await connection.ExecuteScalarAsync<bool>(
                    SqlQueries.TestCases.Exists, new { p_id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(ExistsAsync));
                throw;
            }
        }

        // ── PRIVATE: NULLABLE PARAMETER HELPERS ──────────────────────────────
        private static void AddNullableUuid(NpgsqlCommand cmd, string name, Guid? value) =>
            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Uuid,
                value.HasValue ? (object)value.Value : DBNull.Value);

        private static void AddNullableJsonb(NpgsqlCommand cmd, string name, string? value) =>
            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Jsonb,
                value is not null ? (object)value : DBNull.Value);

        private static void AddNullableText(NpgsqlCommand cmd, string name, string? value) =>
            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Text,
                value is not null ? (object)value : DBNull.Value);

        private static void AddNullableInteger(NpgsqlCommand cmd, string name, int? value) =>
            cmd.Parameters.AddWithValue(name, NpgsqlDbType.Integer,
                value.HasValue ? (object)value.Value : DBNull.Value);

        // ── PRIVATE: DB MODEL ─────────────────────────────────────────────────
        private class TestCaseDb
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Endpoint { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? Steps { get; set; }   // JSONB → string from Dapper
            public string ExpectedResult { get; set; } = string.Empty;
            public int Priority { get; set; }
            public int Status { get; set; }
            public Guid? AssignedTo { get; set; }
            public bool IsAiGenerated { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public Guid CreatedBy { get; set; }
            public Guid? UpdatedBy { get; set; }
            public bool IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public Guid? DeletedBy { get; set; }
            public string? RequestHeaders { get; set; }
            public string? RequestBody { get; set; }
            public int? ExpectedStatusCode { get; set; }
            public string? ExpectedBodyContains { get; set; }
            public int? ExpectedResponseTimeMs { get; set; }
        }

        // ── PRIVATE: MAPPER ───────────────────────────────────────────────────
        // FIX: Deserialise Steps as List<ExecutableStep>, with fallback for legacy string arrays
        private static TestCase MapToTestCase(TestCaseDb db)
        {
            List<ExecutableStep> steps = new();

            if (!string.IsNullOrWhiteSpace(db.Steps))
            {
                try
                {
                    // Try new format first (array of objects)
                    steps = JsonSerializer.Deserialize<List<ExecutableStep>>(db.Steps, _jsonOptions)
                            ?? new();
                }
                catch
                {
                    // Legacy: was a string array — leave steps empty, GetExecutableSteps() handles fallback
                    steps = new();
                }
            }

            return new TestCase
            {
                Id = db.Id,
                ProjectId = db.ProjectId,
                Endpoint = db.Endpoint,
                Method = db.Method,
                Title = db.Title,
                Steps = steps,
                ExpectedResult = db.ExpectedResult,
                Priority = (TestCasePriority)db.Priority,
                Status = (TestCaseStatus)db.Status,
                AssignedTo = db.AssignedTo,
                IsAiGenerated = db.IsAiGenerated,
                CreatedAt = db.CreatedAt,
                UpdatedAt = db.UpdatedAt,
                CreatedBy = db.CreatedBy,
                UpdatedBy = db.UpdatedBy,
                IsDeleted = db.IsDeleted,
                DeletedAt = db.DeletedAt,
                DeletedBy = db.DeletedBy,
                RequestHeaders = db.RequestHeaders,
                RequestBody = db.RequestBody,
                ExpectedStatusCode = db.ExpectedStatusCode,
                ExpectedBodyContains = db.ExpectedBodyContains,
                ExpectedResponseTimeMs = db.ExpectedResponseTimeMs
            };
        }
    }
}