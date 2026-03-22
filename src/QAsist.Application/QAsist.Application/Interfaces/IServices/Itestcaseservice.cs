using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Enums;

namespace QAsist.Application.Interfaces.IServices
{
    public interface ITestCaseService
    {
        // ── READ ─────────────────────────────────────────────────────────────────
        Task<TestCaseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IEnumerable<TestCaseSummaryDto>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default);

        Task<(IEnumerable<TestCaseSummaryDto> TestCases, int TotalCount)> GetPagedAsync(
            Guid projectId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<TestCaseSummaryDto>> GetByStatusAsync(
            Guid projectId,
            TestCaseStatus status,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<TestCaseSummaryDto>> GetAiGeneratedAsync(
            Guid projectId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<TestCaseSummaryDto>> GetByAssigneeAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<TestCaseSummaryDto>> SearchAsync(
            Guid projectId,
            string searchTerm,
            CancellationToken cancellationToken = default);

        Task<TestCaseStatisticsDto> GetStatisticsAsync(
            Guid projectId,
            CancellationToken cancellationToken = default);

        // ── WRITE ────────────────────────────────────────────────────────────────
        Task<TestCaseDto> CreateAsync(
            CreateTestCaseDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<TestCaseDto> UpdateAsync(
            UpdateTestCaseDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task UpdateStatusAsync(
            Guid id,
            UpdateTestCaseStatusDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task AssignAsync(
            Guid id,
            AssignTestCaseDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}