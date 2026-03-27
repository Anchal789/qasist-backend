using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Application.Interfaces.IServices
{
    public interface IExecutionService
    {
        Task<ExecutionBatchDto> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<StepResultDto> Results, int TotalCount)> GetResultsAsync(Guid batchId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<(IEnumerable<ExecutionBatchDto> Batches, int TotalCount)> GetHistoryAsync(Guid suiteId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
