using Dapper;
using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Infrastructure.Persistence;
using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Infrastructure.Repository
{
    public class TestSuiteMappingRepository : ITestSuiteMappingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<TestSuiteMappingRepository> _logger;

        public TestSuiteMappingRepository(
            IDbConnectionFactory connectionFactory,
            ILogger<TestSuiteMappingRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ── GET BY SUITE ──────────────────────────────────────────────────────
        public async Task<IEnumerable<TestSuiteTestCase>> GetBySuiteAsync(
            Guid suiteId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    SELECT id, test_suite_id, test_case_id, ""order"",
                           is_enabled, created_by, created_at,
                           updated_by, updated_at, is_deleted
                    FROM test_suite_test_cases
                    WHERE test_suite_id = @SuiteId AND is_deleted = false
                    ORDER BY ""order""";

                var results = await connection.QueryAsync<TestSuiteTestCase>(
                    sql, new { SuiteId = suiteId });

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetBySuiteAsync {SuiteId}", suiteId);
                throw;
            }
        }

        // ── GET BY SUITE WITH DETAILS ─────────────────────────────────────────
        public async Task<IEnumerable<SuiteTestCaseMappingDto>> GetBySuiteWithDetailsAsync(
            Guid suiteId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                // JOIN with test_cases to get title, endpoint, method etc.
                const string sql = @"
                    SELECT
                        m.id            AS MappingId,
                        m.test_suite_id AS TestSuiteId,
                        m.test_case_id  AS TestCaseId,
                        tc.title        AS TestCaseTitle,
                        tc.endpoint     AS Endpoint,
                        tc.method       AS Method,
                        m.""order""     AS ""Order"",
                        m.is_enabled    AS IsEnabled,
                        tc.priority     AS Priority,
                        tc.status       AS Status
                    FROM test_suite_test_cases m
                    INNER JOIN test_cases tc
                        ON tc.id = m.test_case_id
                        AND tc.is_deleted = false
                    WHERE m.test_suite_id = @SuiteId
                      AND m.is_deleted = false
                    ORDER BY m.""order""";

                var results = await connection.QueryAsync<SuiteTestCaseMappingDto>(
                    sql, new { SuiteId = suiteId });

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetBySuiteWithDetailsAsync {SuiteId}", suiteId);
                throw;
            }
        }

        // ── EXISTS ────────────────────────────────────────────────────────────
        public async Task<bool> ExistsAsync(
            Guid suiteId, Guid testCaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    SELECT EXISTS(
                        SELECT 1 FROM test_suite_test_cases
                        WHERE test_suite_id = @SuiteId
                          AND test_case_id  = @TestCaseId
                          AND is_deleted    = false
                    )";

                return await connection.ExecuteScalarAsync<bool>(
                    sql, new { SuiteId = suiteId, TestCaseId = testCaseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: ExistsAsync {SuiteId}/{TestCaseId}",
                    suiteId, testCaseId);
                throw;
            }
        }

        // ── GET COUNT ─────────────────────────────────────────────────────────
        public async Task<int> GetCountAsync(
            Guid suiteId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                return await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM test_suite_test_cases
                      WHERE test_suite_id = @SuiteId AND is_deleted = false",
                    new { SuiteId = suiteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: GetCountAsync {SuiteId}", suiteId);
                throw;
            }
        }

        // ── ADD RANGE ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<Guid>> AddRangeAsync(
            Guid suiteId,
            List<TestCaseSuiteMappingDto> mappings,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                var createdIds = new List<Guid>();

                foreach (var mapping in mappings)
                {
                    const string sql = @"
                        INSERT INTO test_suite_test_cases
                            (id, test_suite_id, test_case_id, ""order"",
                             is_enabled, created_by, created_at)
                        VALUES
                            (uuid_generate_v4(), @SuiteId, @TestCaseId,
                             @Order, @IsEnabled, @CreatedBy, NOW())
                        ON CONFLICT (test_suite_id, test_case_id) DO NOTHING
                        RETURNING id";

                    var id = await connection.ExecuteScalarAsync<Guid?>(sql, new
                    {
                        SuiteId = suiteId,
                        TestCaseId = mapping.TestCaseId,
                        Order = mapping.Order,
                        IsEnabled = mapping.IsEnabled,
                        CreatedBy = userId
                    });

                    if (id.HasValue)
                        createdIds.Add(id.Value);
                    else
                        _logger.LogWarning(
                            "TestCase {TestCaseId} already mapped to suite {SuiteId} — skipped.",
                            mapping.TestCaseId, suiteId);
                }

                _logger.LogDebug(
                    "Added {Count} test cases to suite {SuiteId}",
                    createdIds.Count, suiteId);

                return createdIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: AddRangeAsync {SuiteId}", suiteId);
                throw;
            }
        }

        // ── REMOVE BY MAPPING ID ──────────────────────────────────────────────
        public async Task<bool> RemoveAsync(
            Guid mappingId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    UPDATE test_suite_test_cases SET
                        is_deleted = true,
                        deleted_at = NOW(),
                        deleted_by = @DeletedBy
                    WHERE id = @Id AND is_deleted = false
                    RETURNING TRUE";

                var result = await connection.ExecuteScalarAsync<bool?>(
                    sql, new { Id = mappingId, DeletedBy = userId });

                return result ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: RemoveAsync {MappingId}", mappingId);
                throw;
            }
        }

        // ── REMOVE BY TEST CASE ───────────────────────────────────────────────
        public async Task<bool> RemoveByTestCaseAsync(
            Guid suiteId, Guid testCaseId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                const string sql = @"
                    UPDATE test_suite_test_cases SET
                        is_deleted = true,
                        deleted_at = NOW(),
                        deleted_by = @DeletedBy
                    WHERE test_suite_id = @SuiteId
                      AND test_case_id  = @TestCaseId
                      AND is_deleted    = false
                    RETURNING TRUE";

                var result = await connection.ExecuteScalarAsync<bool?>(
                    sql, new
                    {
                        SuiteId = suiteId,
                        TestCaseId = testCaseId,
                        DeletedBy = userId
                    });

                return result ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: RemoveByTestCaseAsync {SuiteId}/{TestCaseId}",
                    suiteId, testCaseId);
                throw;
            }
        }

        // ── REORDER ───────────────────────────────────────────────────────────
        public async Task ReorderAsync(
            Guid suiteId,
            List<TestCaseOrderDto> order,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _connectionFactory
                    .CreateConnectionAsync(cancellationToken);

                foreach (var item in order)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE test_suite_test_cases
                          SET ""order"" = @Order, updated_at = NOW()
                          WHERE id = @Id AND test_suite_id = @SuiteId
                            AND is_deleted = false",
                        new
                        {
                            Id = item.MappingId,
                            SuiteId = suiteId,
                            Order = item.Order
                        });
                }

                _logger.LogDebug(
                    "Reordered {Count} test cases in suite {SuiteId}",
                    order.Count, suiteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Repository error: ReorderAsync {SuiteId}", suiteId);
                throw;
            }
        }
    }
}
