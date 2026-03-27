using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QAsist.Infrastructure.Repository
{
    /// <summary>
    /// Week 8 — Repository for TestSuite aggregate.
    /// Pattern matches your existing ProjectRepository + TestCaseRepository.
    /// Uses Dapper + raw SQL (no stored functions for new engine tables).
    /// </summary>
    public class TestSuiteRepository : ITestSuiteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<TestSuiteRepository> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TestSuiteRepository(
            IDbConnectionFactory connectionFactory,
            ILogger<TestSuiteRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<TestSuite?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    SELECT id, project_id, name, description, variables,
                           parallel_cases, is_active,
                           created_by, created_at, updated_by, updated_at,
                           is_deleted
                    FROM test_suites
                    WHERE id = @Id AND is_deleted = false";

                var db = await connection.QueryFirstOrDefaultAsync<TestSuiteDb>(
                    sql, new { Id = id });

                return db is null ? null : MapToEntity(db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetByIdAsync {Id}", id);
                throw;
            }
        }

        // ── GET WITH FULL TREE ────────────────────────────────────────────────
        public async Task<TestSuite?> GetWithDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                // Load suite
                const string suiteSql = @"
                    SELECT id, project_id, name, description, variables,
                           parallel_cases, is_active,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM test_suites
                    WHERE id = @Id AND is_deleted = false";

                var suiteDb = await connection.QueryFirstOrDefaultAsync<TestSuiteDb>(
                    suiteSql, new { Id = id });

                if (suiteDb is null) return null;

                var suite = MapToEntity(suiteDb);

                // Load cases (ordered)
                const string casesSql = @"
                    SELECT id, test_suite_id, name, order_index, is_enabled, tags,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM test_cases_suite
                    WHERE test_suite_id = @SuiteId AND is_deleted = false
                    ORDER BY order_index";

                var caseDbs = await connection.QueryAsync<TestCaseSuiteDb>(
                    casesSql, new { SuiteId = id });

                // Load steps for all cases in one query
                var caseIds = caseDbs.Select(c => c.Id).ToArray();

                IEnumerable<TestStepDb> stepDbs = new List<TestStepDb>();
                IEnumerable<AssertionDb> assertionDbs = new List<AssertionDb>();
                IEnumerable<ExtractionDb> extractionDbs = new List<ExtractionDb>();

                if (caseIds.Length > 0)
                {
                    const string stepsSql = @"
                        SELECT id, test_case_id, name, order_index, http_method,
                               url, request_headers, request_body, auth_config,
                               timeout_ms, retry_count, is_enabled, description,
                               created_by, created_at, updated_by, updated_at, is_deleted
                        FROM test_steps
                        WHERE test_case_id = ANY(@CaseIds) AND is_deleted = false
                        ORDER BY order_index";

                    stepDbs = await connection.QueryAsync<TestStepDb>(
                        stepsSql, new { CaseIds = caseIds });

                    var stepIds = stepDbs.Select(s => s.Id).ToArray();

                    if (stepIds.Length > 0)
                    {
                        const string assertionsSql = @"
                            SELECT id, test_step_id, assertion_type, field,
                                   operator, expected_value, order_index, is_required,
                                   created_by, created_at, is_deleted
                            FROM assertions
                            WHERE test_step_id = ANY(@StepIds) AND is_deleted = false
                            ORDER BY order_index";

                        assertionDbs = await connection.QueryAsync<AssertionDb>(
                            assertionsSql, new { StepIds = stepIds });

                        const string extractionsSql = @"
                            SELECT id, test_step_id, variable_name, source,
                                   json_path, header_name, default_value,
                                   created_by, created_at, is_deleted
                            FROM extractions
                            WHERE test_step_id = ANY(@StepIds) AND is_deleted = false";

                        extractionDbs = await connection.QueryAsync<ExtractionDb>(
                            extractionsSql, new { StepIds = stepIds });
                    }
                }

                // Assemble tree
                var stepsByCase = stepDbs
                    .GroupBy(s => s.TestCaseId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var assertionsByStep = assertionDbs
                    .GroupBy(a => a.TestStepId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var extractionsByStep = extractionDbs
                    .GroupBy(e => e.TestStepId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                suite.TestCases = caseDbs.Select(c =>
                {
                    var testCase = MapCaseToEntity(c);
                    var steps = stepsByCase.GetValueOrDefault(c.Id, new());

                    testCase.Steps = steps.Select(s =>
                    {
                        var step = MapStepToEntity(s);
                        step.Assertions = assertionsByStep
                            .GetValueOrDefault(s.Id, new())
                            .Select(MapAssertionToEntity).ToList();
                        step.Extractions = extractionsByStep
                            .GetValueOrDefault(s.Id, new())
                            .Select(MapExtractionToEntity).ToList();
                        return step;
                    }).ToList();

                    return testCase;
                }).ToList();

                _logger.LogDebug(
                    "Loaded suite {Id} with {Cases} cases, {Steps} steps",
                    id, suite.TestCases.Count,
                    suite.TestCases.Sum(c => c.Steps.Count));

                return suite;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetWithDetailsAsync {Id}", id);
                throw;
            }
        }

        // ── GET BY PROJECT ────────────────────────────────────────────────────
        public async Task<IEnumerable<TestSuite>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    SELECT id, project_id, name, description, variables,
                           parallel_cases, is_active,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM test_suites
                    WHERE project_id = @ProjectId AND is_deleted = false
                    ORDER BY created_at DESC";

                var results = await connection.QueryAsync<TestSuiteDb>(
                    sql, new { ProjectId = projectId });

                return results.Select(MapToEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetByProjectAsync {ProjectId}", projectId);
                throw;
            }
        }

        // ── GET PAGED ─────────────────────────────────────────────────────────
        public async Task<(IEnumerable<TestSuite> Suites, int TotalCount)> GetPagedAsync(
            Guid projectId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string dataSql = @"
                    SELECT id, project_id, name, description, variables,
                           parallel_cases, is_active,
                           created_by, created_at, updated_by, updated_at, is_deleted
                    FROM test_suites
                    WHERE project_id = @ProjectId AND is_deleted = false
                    ORDER BY created_at DESC
                    LIMIT @PageSize OFFSET @Offset";

                const string countSql = @"
                    SELECT COUNT(*) FROM test_suites
                    WHERE project_id = @ProjectId AND is_deleted = false";

                var suites = await connection.QueryAsync<TestSuiteDb>(
                    dataSql,
                    new
                    {
                        ProjectId = projectId,
                        PageSize = pageSize,
                        Offset = (pageNumber - 1) * pageSize
                    });

                var total = await connection.ExecuteScalarAsync<int>(
                    countSql, new { ProjectId = projectId });

                return (suites.Select(MapToEntity), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetPagedAsync {ProjectId}", projectId);
                throw;
            }
        }

        // ── EXISTS ────────────────────────────────────────────────────────────
        public async Task<bool> ExistsAsync(
            Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                return await connection.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM test_suites WHERE id=@Id AND is_deleted=false)",
                    new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repository error: ExistsAsync {Id}", id);
                throw;
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<Guid> CreateAsync(
            TestSuite suite, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);
                var npgsql = (NpgsqlConnection)connection;

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO test_suites
                        (id, project_id, name, description, variables,
                         parallel_cases, is_active, created_by, created_at)
                    VALUES
                        (@id, @project_id, @name, @description, @variables,
                         @parallel_cases, @is_active, @created_by, NOW())
                    RETURNING id";

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, suite.Id);
                cmd.Parameters.AddWithValue("project_id", NpgsqlDbType.Uuid, suite.ProjectId);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, suite.Name);
                cmd.Parameters.AddWithValue("description", NpgsqlDbType.Text,
                    (object?)suite.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("variables", NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(suite.Variables, _jsonOptions));
                cmd.Parameters.AddWithValue("parallel_cases", NpgsqlDbType.Boolean, suite.ParallelCases);
                cmd.Parameters.AddWithValue("is_active", NpgsqlDbType.Boolean, suite.IsActive);
                cmd.Parameters.AddWithValue("created_by", NpgsqlDbType.Uuid, userId);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return (Guid)result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: CreateAsync suite={Name}", suite.Name);
                throw;
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<bool> UpdateAsync(
            TestSuite suite, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);
                var npgsql = (NpgsqlConnection)connection;

                await using var cmd = npgsql.CreateCommand();
                cmd.CommandText = @"
                    UPDATE test_suites SET
                        name           = @name,
                        description    = @description,
                        variables      = @variables,
                        parallel_cases = @parallel_cases,
                        is_active      = @is_active,
                        updated_by     = @updated_by,
                        updated_at     = NOW()
                    WHERE id = @id AND is_deleted = false
                    RETURNING TRUE";

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, suite.Id);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, suite.Name);
                cmd.Parameters.AddWithValue("description", NpgsqlDbType.Text,
                    (object?)suite.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("variables", NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(suite.Variables, _jsonOptions));
                cmd.Parameters.AddWithValue("parallel_cases", NpgsqlDbType.Boolean, suite.ParallelCases);
                cmd.Parameters.AddWithValue("is_active", NpgsqlDbType.Boolean, suite.IsActive);
                cmd.Parameters.AddWithValue("updated_by", NpgsqlDbType.Uuid, userId);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result != null ? Convert.ToBoolean(result) : false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: UpdateAsync {Id}", suite.Id);
                throw;
            }
        }

        // ── DELETE (soft) ─────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(
            Guid id, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    UPDATE test_suites SET
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
                    "Repository error: DeleteAsync {Id}", id);
                throw;
            }
        }

        // ── PRIVATE: DB models ────────────────────────────────────────────────
        private class TestSuiteDb
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Variables { get; set; }  // JSONB → string
            public bool ParallelCases { get; set; }
            public bool IsActive { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid? UpdatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        private class TestCaseSuiteDb
        {
            public Guid Id { get; set; }
            public Guid TestSuiteId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int OrderIndex { get; set; }
            public bool IsEnabled { get; set; }
            public string? Tags { get; set; } // array as JSON
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid? UpdatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        private class TestStepDb
        {
            public Guid Id { get; set; }
            public Guid TestCaseId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int OrderIndex { get; set; }
            public string HttpMethod { get; set; } = "GET";
            public string Url { get; set; } = string.Empty;
            public string? RequestHeaders { get; set; } // JSONB
            public string? RequestBody { get; set; }
            public string? AuthConfig { get; set; }   // JSONB
            public int TimeoutMs { get; set; }
            public int RetryCount { get; set; }
            public bool IsEnabled { get; set; }
            public string? Description { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid? UpdatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        private class AssertionDb
        {
            public Guid Id { get; set; }
            public Guid TestStepId { get; set; }
            public int AssertionType { get; set; }
            public string? Field { get; set; }
            public string? Operator { get; set; }
            public string? ExpectedValue { get; set; }
            public int OrderIndex { get; set; }
            public bool IsRequired { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        private class ExtractionDb
        {
            public Guid Id { get; set; }
            public Guid TestStepId { get; set; }
            public string VariableName { get; set; } = string.Empty;
            public int Source { get; set; }
            public string? JsonPath { get; set; }
            public string? HeaderName { get; set; }
            public string? DefaultValue { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        // ── PRIVATE: Mappers ──────────────────────────────────────────────────
        private static TestSuite MapToEntity(TestSuiteDb db)
        {
            Dictionary<string, string> vars;
            try
            {
                vars = string.IsNullOrWhiteSpace(db.Variables)
                    ? new()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(
                        db.Variables, _jsonOptions) ?? new();
            }
            catch { vars = new(); }

            return new TestSuite
            {
                Id = db.Id,
                ProjectId = db.ProjectId,
                Name = db.Name,
                Description = db.Description,
                Variables = vars,
                ParallelCases = db.ParallelCases,
                IsActive = db.IsActive,
                CreatedBy = db.CreatedBy,
                CreatedAt = db.CreatedAt,
                UpdatedBy = db.UpdatedBy,
                UpdatedAt = db.UpdatedAt,
                IsDeleted = db.IsDeleted,
                TestCases = new List<TestCaseSuite>()
            };
        }

        private static TestCaseSuite MapCaseToEntity(TestCaseSuiteDb db)
        {
            List<string> tags;
            try
            {
                tags = string.IsNullOrWhiteSpace(db.Tags)
                    ? new()
                    : JsonSerializer.Deserialize<List<string>>(db.Tags) ?? new();
            }
            catch { tags = new(); }

            return new TestCaseSuite
            {
                Id = db.Id,
                TestSuiteId = db.TestSuiteId,
                Name = db.Name,
                OrderIndex = db.OrderIndex,
                IsEnabled = db.IsEnabled,
                Tags = tags,
                CreatedBy = db.CreatedBy,
                CreatedAt = db.CreatedAt,
                UpdatedBy = db.UpdatedBy,
                UpdatedAt = db.UpdatedAt,
                IsDeleted = db.IsDeleted,
                Steps = new List<TestStep>()
            };
        }

        private static TestStep MapStepToEntity(TestStepDb db)
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

            Domain.ValueObjects.AuthConfig? authConfig = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(db.AuthConfig))
                    authConfig = JsonSerializer.Deserialize<Domain.ValueObjects.AuthConfig>(
                        db.AuthConfig, _jsonOptions);
            }
            catch { }

            return new TestStep
            {
                Id = db.Id,
                TestCaseId = db.TestCaseId,
                Name = db.Name,
                OrderIndex = db.OrderIndex,
                Method = Enum.Parse<Domain.Enums.HttpMethod>(db.HttpMethod, true),
                Url = db.Url,
                RequestHeaders = headers,
                RequestBody = db.RequestBody,
                AuthConfig = authConfig,
                TimeoutMs = db.TimeoutMs,
                RetryCount = db.RetryCount,
                IsEnabled = db.IsEnabled,
                Description = db.Description,
                CreatedBy = db.CreatedBy,
                CreatedAt = db.CreatedAt,
                UpdatedBy = db.UpdatedBy,
                UpdatedAt = db.UpdatedAt,
                IsDeleted = db.IsDeleted,
                Assertions = new List<Assertion>(),
                Extractions = new List<Extraction>()
            };
        }

        private static Assertion MapAssertionToEntity(AssertionDb db) => new()
        {
            Id = db.Id,
            TestStepId = db.TestStepId,
            AssertionType = (Domain.Enums.AssertionType)db.AssertionType,
            Field = db.Field,
            Operator = db.Operator,
            ExpectedValue = db.ExpectedValue,
            OrderIndex = db.OrderIndex,
            IsRequired = db.IsRequired,
            CreatedBy = db.CreatedBy,
            CreatedAt = db.CreatedAt,
            IsDeleted = db.IsDeleted
        };

        private static Extraction MapExtractionToEntity(ExtractionDb db) => new()
        {
            Id = db.Id,
            TestStepId = db.TestStepId,
            VariableName = db.VariableName,
            Source = (Domain.Enums.ExtractionSource)db.Source,
            JsonPath = db.JsonPath,
            HeaderName = db.HeaderName,
            DefaultValue = db.DefaultValue,
            CreatedBy = db.CreatedBy,
            CreatedAt = db.CreatedAt,
            IsDeleted = db.IsDeleted
        };
    }
}
