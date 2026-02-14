using Dapper;
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

        public TestCaseRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<TestCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_id = id };
            var result = await connection.QueryFirstOrDefaultAsync<TestCaseDb>(
                SqlQueries.TestCases.GetById,
                parameters);

            return result != null ? MapToTestCase(result) : null;
        }

        public async Task<IEnumerable<TestCase>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_project_id = projectId };
            var results = await connection.QueryAsync<TestCaseDb>(
                SqlQueries.TestCases.GetByProject,
                parameters);

            return results.Select(MapToTestCase);
        }

        public async Task<(IEnumerable<TestCase> TestCases, int TotalCount)> GetPagedAsync(
            Guid projectId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_project_id = projectId,
                p_page_number = pageNumber,
                p_page_size = pageSize
            };

            var testCases = await connection.QueryAsync<TestCaseDb>(
                SqlQueries.TestCases.GetPaged,
                parameters);

            var countParams = new { p_project_id = projectId };
            var totalCount = await connection.ExecuteScalarAsync<int>(
                SqlQueries.TestCases.GetCount,
                countParams);

            return (testCases.Select(MapToTestCase), totalCount);
        }

        public async Task<IEnumerable<TestCase>> GetByStatusAsync(
            Guid projectId,
            TestCaseStatus status,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_project_id = projectId,
                p_status = (int)status
            };

            var results = await connection.QueryAsync<TestCaseDb>(
                SqlQueries.TestCases.GetByStatus,
                parameters);

            return results.Select(MapToTestCase);
        }

        public async Task<IEnumerable<TestCase>> GetAiGeneratedAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_project_id = projectId };
            var results = await connection.QueryAsync<TestCaseDb>(
                SqlQueries.TestCases.GetAiGenerated,
                parameters);

            return results.Select(MapToTestCase);
        }

        public async Task<IEnumerable<TestCase>> GetByAssigneeAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_user_id = userId };
            var results = await connection.QueryAsync<TestCaseDb>(
                SqlQueries.TestCases.GetByAssignee,
                parameters);

            return results.Select(MapToTestCase);
        }

        public async Task<Guid> CreateAsync(TestCase testCase, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var stepsJson = JsonSerializer.Serialize(testCase.Steps);

            var parameters = new
            {
                p_id = testCase.Id,
                p_project_id = testCase.ProjectId,
                p_endpoint = testCase.Endpoint,
                p_method = testCase.Method,
                p_title = testCase.Title,
                p_steps = stepsJson,
                p_expected_result = testCase.ExpectedResult,
                p_priority = (int)testCase.Priority,
                p_status = (int)testCase.Status,
                p_assigned_to = testCase.AssignedTo,
                p_is_ai_generated = testCase.IsAiGenerated,
                p_created_by = userId
            };

            return await connection.ExecuteScalarAsync<Guid>(
                SqlQueries.TestCases.Create,
                parameters);
        }

        public async Task<int> BulkCreateAsync(
            List<GeneratedTestCaseDto> testCases,
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var testCasesJson = JsonSerializer.Serialize(testCases);

            var parameters = new
            {
                p_test_cases = testCasesJson,
                p_project_id = projectId,
                p_created_by = userId
            };

            return await connection.ExecuteScalarAsync<int>(
                SqlQueries.TestCases.BulkCreate,
                parameters);
        }

        public async Task<bool> UpdateAsync(TestCase testCase, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var stepsJson = JsonSerializer.Serialize(testCase.Steps);

            var parameters = new
            {
                p_id = testCase.Id,
                p_endpoint = testCase.Endpoint,
                p_method = testCase.Method,
                p_title = testCase.Title,
                p_steps = stepsJson,
                p_expected_result = testCase.ExpectedResult,
                p_priority = (int)testCase.Priority,
                p_status = (int)testCase.Status,
                p_assigned_to = testCase.AssignedTo,
                p_updated_by = userId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.TestCases.Update,
                parameters);
        }

        public async Task<bool> UpdateStatusAsync(Guid id, TestCaseStatus status, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = id,
                p_status = (int)status,
                p_updated_by = userId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.TestCases.UpdateStatus,
                parameters);
        }

        public async Task<bool> AssignAsync(Guid id, Guid assignedTo, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = id,
                p_assigned_to = assignedTo,
                p_updated_by = userId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.TestCases.Assign,
                parameters);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = id,
                p_deleted_by = userId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.TestCases.Delete,
                parameters);
        }

        public async Task<int> BulkDeleteByProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_project_id = projectId,
                p_deleted_by = userId
            };

            return await connection.ExecuteScalarAsync<int>(
                SqlQueries.TestCases.BulkDeleteByProject,
                parameters);
        }

        public async Task<TestCaseStatisticsDto> GetStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_project_id = projectId };
            return await connection.QueryFirstOrDefaultAsync<TestCaseStatisticsDto>(
                SqlQueries.TestCases.GetStatistics,
                parameters) ?? new TestCaseStatisticsDto();
        }

        public async Task<IEnumerable<EndpointCoverageDto>> GetEndpointCoverageAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_project_id = projectId };
            return await connection.QueryAsync<EndpointCoverageDto>(
                SqlQueries.TestCases.GetEndpointCoverage,
                parameters);
        }

        public async Task<IEnumerable<TestCase>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_project_id = projectId,
                p_search_term = searchTerm
            };

            var results = await connection.QueryAsync<TestCaseDb>(
                SqlQueries.TestCases.Search,
                parameters);

            return results.Select(MapToTestCase);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_id = id };
            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.TestCases.Exists,
                parameters);
        }

        // Helper Methods
        private TestCase MapToTestCase(TestCaseDb db)
        {
            List<string> steps;
            try
            {
                steps = JsonSerializer.Deserialize<List<string>>(db.Steps ?? "[]") ?? new List<string>();
            }
            catch
            {
                steps = new List<string>();
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
                UpdatedBy = db.UpdatedBy
            };
        }

        // Database model class
        private class TestCaseDb
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Endpoint { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? Steps { get; set; }
            public string ExpectedResult { get; set; } = string.Empty;
            public int Priority { get; set; }
            public int Status { get; set; }
            public Guid? AssignedTo { get; set; }
            public bool IsAiGenerated { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public Guid CreatedBy { get; set; }
            public Guid? UpdatedBy { get; set; }
        }
    }
}
