using QAsist.Application.DTOs;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface ITestCaseRepository
    {
        // ── GET (all existing — unchanged) ────────────────────────────────────────
        Task<TestCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<TestCase> TestCases, int TotalCount)> GetPagedAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetByStatusAsync(Guid projectId, TestCaseStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetAiGeneratedAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestCase>> GetByAssigneeAsync(Guid userId, CancellationToken cancellationToken = default);

        // ── CREATE (existing + no change) ─────────────────────────────────────────
        Task<Guid> CreateAsync(TestCase testCase, Guid userId, CancellationToken cancellationToken = default);
        Task<int> BulkCreateAsync(List<GeneratedTestCaseDto> testCases, Guid projectId, Guid userId, CancellationToken cancellationToken = default);

        // ── UPDATE (existing + no change) ─────────────────────────────────────────
        Task<bool> UpdateAsync(TestCase testCase, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(Guid id, TestCaseStatus status, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> AssignAsync(Guid id, Guid assignedTo, Guid userId, CancellationToken cancellationToken = default);

        // ── DELETE (existing + no change) ─────────────────────────────────────────
        Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<int> BulkDeleteByProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

        // ── STATISTICS (existing + no change) ────────────────────────────────────
        Task<TestCaseStatisticsDto> GetStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<EndpointCoverageDto>> GetEndpointCoverageAsync(Guid projectId, CancellationToken cancellationToken = default);

        // ── SEARCH (existing + no change) ────────────────────────────────────────
        Task<IEnumerable<TestCase>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

        // ── HELPERS (existing + no change) ───────────────────────────────────────
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

        // ── NEW: Summary list (lightweight — no steps deserialization) ────────────
        Task<IEnumerable<TestCase>> GetSummaryByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    }

    // ── Kept exactly as you have them ─────────────────────────────────────────────

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

    public class EndpointCoverageDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public long TestCount { get; set; }
        public long PassedCount { get; set; }
        public long FailedCount { get; set; }
    }
}
