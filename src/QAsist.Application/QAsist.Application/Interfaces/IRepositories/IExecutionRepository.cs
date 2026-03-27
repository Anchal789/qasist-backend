using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface IExecutionRepository
    {
        // Batches
        Task<Guid> CreateBatchAsync(ExecutionBatch batch, CancellationToken cancellationToken = default);
        Task<bool> UpdateBatchAsync(ExecutionBatch batch, CancellationToken cancellationToken = default);
        Task<ExecutionBatch?> GetBatchByIdAsync(Guid batchId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<ExecutionBatch> Batches, int TotalCount)> GetBatchHistoryAsync(
            Guid suiteId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Results
        Task SaveStepResultAsync(ExecutionResult result, CancellationToken cancellationToken = default);

        Task<IEnumerable<ExecutionResult>> GetResultsByBatchAsync(
            Guid batchId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<ExecutionResult> Results, int TotalCount)> GetResultsByBatchPagedAsync(
            Guid batchId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
