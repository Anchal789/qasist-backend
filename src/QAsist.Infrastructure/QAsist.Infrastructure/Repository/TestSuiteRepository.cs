using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Infrastructure.Persistence;
using System.Text.Json;

namespace QAsist.Infrastructure.Repository
{
    /// <summary>
    /// Week 8 — Repository for TestSuite aggregate.
    /// FIX: GetWithDetailsAsync now reads steps from test_cases.steps JSONB
    ///      instead of test_steps table (which is empty — engine's own table).
    ///      Steps are deserialized from ExecutableStep JSONB into TestStep domain objects.
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
                _logger.LogError(ex, "Repository error: GetByIdAsync {Id}", id);
                throw;
            }
        }

        // ── GET WITH FULL TREE ────────────────────────────────────────────────
        // FIX: Reads cases from test_suite_test_cases (mapping table) joined to
        //      test_cases, then deserializes steps directly from test_cases.steps
        //      JSONB column. Does NOT query test_steps table (that table is only
        //      populated by the engine's own native flow, not by the API).
        public async Task<TestSuite?> GetWithDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                // 1. Load suite
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

                // 2. Load cases via mapping table joined to test_cases.
                //    Also pulls test_cases.steps JSONB directly.
                const string casesSql = @"
                    SELECT
                        m.id                AS id,
                        m.test_suite_id     AS test_suite_id,
                        tc.id               AS linked_test_case_id,
                        tc.title            AS name,
                        tc.steps            AS steps_json,
                        m.""order""         AS order_index,
                        m.is_enabled        AS is_enabled,
                        m.created_by        AS created_by,
                        m.created_at        AS created_at,
                        m.updated_by        AS updated_by,
                        m.updated_at        AS updated_at,
                        m.is_deleted        AS is_deleted
                    FROM test_suite_test_cases m
                    INNER JOIN test_cases tc
                        ON tc.id = m.test_case_id AND tc.is_deleted = false
                    WHERE m.test_suite_id = @SuiteId
                      AND m.is_deleted = false
                    ORDER BY m.""order""";

                var caseDbs = (await connection.QueryAsync<TestCaseSuiteDb>(
                    casesSql, new { SuiteId = id })).ToList();

                _logger.LogDebug(
                    "GetWithDetailsAsync: loaded {Count} mapped cases for suite {Id}",
                    caseDbs.Count, id);

                // 3. For each case, deserialize steps from JSONB and map to TestStep
                suite.TestCases = caseDbs.Select(c =>
                {
                    var testCase = MapCaseToEntity(c);

                    // Deserialize ExecutableStep list from JSONB stored in test_cases.steps
                    List<ExecutableStep> rawSteps = new();
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(c.StepsJson))
                        {
                            rawSteps = JsonSerializer.Deserialize<List<ExecutableStep>>(
                                c.StepsJson, _jsonOptions) ?? new();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to deserialize steps JSONB for case {CaseId}. StepsJson={Json}",
                            c.LinkedTestCaseId, c.StepsJson);
                    }

                    _logger.LogDebug(
                        "Case '{Name}' ({CaseId}): deserialized {StepCount} steps",
                        c.Name, c.LinkedTestCaseId, rawSteps.Count);

                    // Map ExecutableStep → TestStep (domain entity the engine understands)
                    testCase.Steps = rawSteps
                        .Where(s => s.IsEnabled)   // skip disabled steps
                        .Select((s, idx) => new TestStep
                        {
                            Id = Guid.NewGuid(),              // synthetic — not in DB
                            TestCaseId = c.LinkedTestCaseId,
                            Name = !string.IsNullOrWhiteSpace(s.Name)
                                            ? s.Name
                                            : $"Step {idx + 1}",
                            OrderIndex = idx,
                            Method = Enum.TryParse<Domain.Enums.HttpMethod>(
                                            s.Method, true, out var parsedMethod)
                                            ? parsedMethod
                                            : Domain.Enums.HttpMethod.GET,
                            Url = s.Url ?? string.Empty,
                            RequestHeaders = s.RequestHeaders ?? new(),
                            RequestBody = s.RequestBody,
                            TimeoutMs = s.TimeoutMs > 0 ? s.TimeoutMs : 10_000,
                            RetryCount = s.RetryCount,
                            IsEnabled = true,    // already filtered above
                            CreatedBy = c.CreatedBy,
                            CreatedAt = c.CreatedAt,
                            Assertions = s.Assertions.Select((a, ai) => new Assertion
                            {
                                Id = Guid.NewGuid(),
                                TestStepId = Guid.Empty,   // synthetic
                                AssertionType = Enum.TryParse<Domain.Enums.AssertionType>(
                                                    a.Type, true, out var parsedAt)
                                                    ? parsedAt
                                                    : Domain.Enums.AssertionType.StatusCodeEquals,
                                Field = a.JsonPath,
                                ExpectedValue = a.Expected,
                                OrderIndex = ai,
                                IsRequired = true,
                                CreatedBy = c.CreatedBy,
                                CreatedAt = c.CreatedAt,
                                IsDeleted = false
                            }).ToList(),
                            Extractions = s.Extractions.Select(e => new Extraction
                            {
                                Id = Guid.NewGuid(),
                                TestStepId = Guid.Empty,   // synthetic
                                VariableName = e.Variable ?? string.Empty,
                                Source = Domain.Enums.ExtractionSource.Body,
                                JsonPath = e.JsonPath,
                                DefaultValue = e.DefaultValue,
                                CreatedBy = c.CreatedBy,
                                CreatedAt = c.CreatedAt,
                                IsDeleted = false
                            }).ToList()
                        }).ToList();

                    return testCase;
                }).ToList();

                _logger.LogDebug(
                    "Loaded suite {Id} with {Cases} cases, {Steps} total steps",
                    id,
                    suite.TestCases.Count,
                    suite.TestCases.Sum(c => c.Steps.Count));

                return suite;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repository error: GetWithDetailsAsync {Id}", id);
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
                return result != null && Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repository error: UpdateAsync {Id}", suite.Id);
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
                _logger.LogError(ex, "Repository error: DeleteAsync {Id}", id);
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
            public Guid LinkedTestCaseId { get; set; }  // tc.id from JOIN
            public string Name { get; set; } = string.Empty;
            public string? StepsJson { get; set; }  // tc.steps JSONB
            public int OrderIndex { get; set; }
            public bool IsEnabled { get; set; }
            public string? Tags { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid? UpdatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
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
            // Tags not used in engine — safe to leave empty
            return new TestCaseSuite
            {
                Id = db.Id,
                TestSuiteId = db.TestSuiteId,
                Name = db.Name,
                OrderIndex = db.OrderIndex,
                IsEnabled = db.IsEnabled,
                Tags = new List<string>(),
                CreatedBy = db.CreatedBy,
                CreatedAt = db.CreatedAt,
                UpdatedBy = db.UpdatedBy,
                UpdatedAt = db.UpdatedAt,
                IsDeleted = db.IsDeleted,
                Steps = new List<TestStep>()
            };
        }
    }
}