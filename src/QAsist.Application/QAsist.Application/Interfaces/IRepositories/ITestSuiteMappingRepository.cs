using QAsist.Domain.Entities;
using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface ITestSuiteMappingRepository
    {
        // ── Read ──────────────────────────────────────────────────────────────
        Task<IEnumerable<TestSuiteTestCase>> GetBySuiteAsync(
            Guid suiteId, CancellationToken cancellationToken = default);

        Task<IEnumerable<SuiteTestCaseMappingDto>> GetBySuiteWithDetailsAsync(
            Guid suiteId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Guid suiteId, Guid testCaseId, CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(
            Guid suiteId, CancellationToken cancellationToken = default);

        // ── Write ─────────────────────────────────────────────────────────────
        Task<IEnumerable<Guid>> AddRangeAsync(
            Guid suiteId,
            List<TestCaseSuiteMappingDto> mappings,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(
            Guid mappingId, Guid userId, CancellationToken cancellationToken = default);

        Task<bool> RemoveByTestCaseAsync(
            Guid suiteId, Guid testCaseId, Guid userId,
            CancellationToken cancellationToken = default);

        Task ReorderAsync(
            Guid suiteId,
            List<TestCaseOrderDto> order,
            CancellationToken cancellationToken = default);
    }
}
