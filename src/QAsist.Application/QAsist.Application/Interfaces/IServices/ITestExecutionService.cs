using QAsist.Application.DTOs;

namespace QAsist.Application.Interfaces.IServices
{
    /// <summary>
    /// API Test Execution Engine - Replaces Postman/REST Assured functionality
    /// </summary>
    public interface ITestExecutionService
    {
        /// <summary>
        /// Execute a batch of API test cases
        /// </summary>
        Task<TestExecutionSummaryDto> ExecuteTestCasesAsync(
            ExecuteTestCasesRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a single test case
        /// </summary>
        Task<TestExecutionResultDto> ExecuteSingleTestAsync(
            ExecutableTestCaseDto testCase,
            string baseUrl,
            string? authToken,
            int timeoutSeconds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate test execution request
        /// </summary>
        Task ValidateExecutionRequestAsync(
            ExecuteTestCasesRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get execution history for a project
        /// </summary>
        Task<IEnumerable<TestExecutionSummaryDto>> GetExecutionHistoryAsync(
            Guid projectId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed execution result
        /// </summary>
        Task<TestExecutionSummaryDto?> GetExecutionDetailsAsync(
            Guid executionBatchId,
            CancellationToken cancellationToken = default);
    }
}
