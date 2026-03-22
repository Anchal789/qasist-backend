using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface ITestStepRepository
    {
        Task<TestStep?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Returns step with its Assertions and Extractions loaded.</summary>
        Task<TestStep?> GetWithAssertionsAndExtractionsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IEnumerable<TestStep>> GetByCaseAsync(Guid testCaseId, CancellationToken cancellationToken = default);

        Task<Guid> CreateAsync(TestStep step, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(TestStep step, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ReorderAsync(Guid testCaseId, List<Guid> orderedIds, CancellationToken cancellationToken = default);
    }
}
