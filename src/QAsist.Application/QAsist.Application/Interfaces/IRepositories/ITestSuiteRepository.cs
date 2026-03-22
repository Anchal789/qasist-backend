using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface ITestSuiteRepository
    {
        // READ
        Task<TestSuite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Returns suite with full child tree: Cases → Steps → Assertions + Extractions.</summary>
        Task<TestSuite?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IEnumerable<TestSuite>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<TestSuite> Suites, int TotalCount)> GetPagedAsync(
            Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

        // WRITE
        Task<Guid> CreateAsync(TestSuite suite, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(TestSuite suite, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    }
}
