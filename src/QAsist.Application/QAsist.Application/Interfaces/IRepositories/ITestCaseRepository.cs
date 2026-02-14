using QAsist.Application.DTOs;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface ITestCaseRepository
    {
        // GET Methods
        Task<TestCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<TestCase> TestCases, int TotalCount)> GetPagedAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetByStatusAsync(Guid projectId, TestCaseStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetAiGeneratedAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetByAssigneeAsync(Guid userId, CancellationToken cancellationToken = default);

        // CREATE Methods
        Task<Guid> CreateAsync(TestCase testCase, Guid userId, CancellationToken cancellationToken = default);
        Task<int> BulkCreateAsync(List<GeneratedTestCaseDto> testCases, Guid projectId, Guid userId, CancellationToken cancellationToken = default);

        // UPDATE Methods
        Task<bool> UpdateAsync(TestCase testCase, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(Guid id, TestCaseStatus status, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> AssignAsync(Guid id, Guid assignedTo, Guid userId, CancellationToken cancellationToken = default);

        // DELETE Methods
        Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<int> BulkDeleteByProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

        // STATISTICS Methods
        Task<TestCaseStatisticsDto> GetStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<EndpointCoverageDto>> GetEndpointCoverageAsync(Guid projectId, CancellationToken cancellationToken = default);

        // SEARCH Methods
        Task<IEnumerable<TestCase>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

        // HELPER Methods
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public class TestCaseStatisticsDto
    {
        public int TotalTestCases { get; set; }
        public int AiGeneratedCount { get; set; }
        public int ManualCount { get; set; }
        public int DraftCount { get; set; }
        public int ActiveCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public int BlockedCount { get; set; }
        public int HighPriorityCount { get; set; }
        public int CriticalPriorityCount { get; set; }
    }

    // Endpoint Coverage DTO
    public class EndpointCoverageDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public long TestCount { get; set; }
        public long PassedCount { get; set; }
        public long FailedCount { get; set; }
    }
}
