using static QAsist.Application.DTOs.Enginedtos;
using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Application.Interfaces.IServices
{
    public interface ISuiteExecutionService
    {
        /// <summary>Execute all test cases in a suite. Returns batchId for polling.</summary>
        Task<SuiteExecutionBatchDto> ExecuteAsync(
            Guid suiteId,
            ExecuteSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>Get batch summary (poll for status).</summary>
        Task<SuiteExecutionBatchDto> GetBatchAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);

        /// <summary>Get paged step results for a batch.</summary>
        Task<(IEnumerable<ExecutionStepResultDto> Results, int Total)> GetResultsAsync(
            Guid batchId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Get execution history for a suite.</summary>
        Task<(IEnumerable<SuiteExecutionBatchDto> Batches, int Total)> GetHistoryAsync(
            Guid suiteId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default);
    }
}
