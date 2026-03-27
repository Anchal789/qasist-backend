using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using System.Text.Json;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Application.Services
{
    public class ExecutionService : IExecutionService
    {
        private readonly IExecutionRepository _executionRepository;
        private readonly ILogger<ExecutionService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ExecutionService(
            IExecutionRepository executionRepository,
            ILogger<ExecutionService> logger)
        {
            _executionRepository = executionRepository;
            _logger = logger;
        }

        // ── GET BATCH ─────────────────────────────────────────────────────────
        public async Task<ExecutionBatchDto> GetBatchAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching execution batch {BatchId}", batchId);

                var batch = await _executionRepository.GetBatchByIdAsync(
                    batchId, cancellationToken);

                if (batch is null)
                    throw new NotFoundException(ResponseMessages.ExecutionNotFound);

                _logger.LogInformation(
                    "Fetched batch {BatchId} status={Status} pass%={Pct}%",
                    batchId, batch.Status, batch.PassPercentage);

                return new ExecutionBatchDto
                {
                    BatchId = batch.Id,
                    SuiteId = batch.TestSuiteId,
                    ProjectId = batch.ProjectId,
                    Status = batch.Status.ToString(),
                    TotalSteps = batch.TotalSteps,
                    PassedSteps = batch.PassedSteps,
                    FailedSteps = batch.FailedSteps,
                    SkippedSteps = batch.SkippedSteps,
                    ErrorSteps = batch.ErrorSteps,
                    PassPercentage = batch.PassPercentage,
                    TotalDurationMs = batch.TotalDurationMs,
                    StartedAt = batch.StartedAt,
                    CompletedAt = batch.CompletedAt
                };
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching execution batch {BatchId}", batchId);
                throw;
            }
        }

        // ── GET RESULTS PAGED ─────────────────────────────────────────────────
        public async Task<(IEnumerable<StepResultDto> Results, int TotalCount)> GetResultsAsync(
            Guid batchId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching results for batch {BatchId}. Page={Page} Size={Size}",
                    batchId, pageNumber, pageSize);

                var batch = await _executionRepository.GetBatchByIdAsync(
                    batchId, cancellationToken);
                if (batch is null)
                    throw new NotFoundException(ResponseMessages.ExecutionNotFound);

                var (results, total) = await _executionRepository
                    .GetResultsByBatchPagedAsync(batchId, pageNumber, pageSize, cancellationToken);

                var dtos = results.Select(r => new StepResultDto
                {
                    Id = r.Id,
                    BatchId = r.BatchId,
                    TestStepId = r.TestStepId,
                    Status = r.Status.ToString(),
                    DurationMs = r.DurationMs,
                    ErrorMessage = r.ErrorMessage,
                    RequestLog = r.RequestLog,
                    ResponseLog = r.ResponseLog,
                    AssertionResults = r.AssertionResults.Select(a => new AssertionResultDto
                    {
                        AssertionType = a.AssertionType.ToString(),
                        Passed = a.Passed,
                        ActualValue = a.ActualValue,
                        ExpectedValue = a.ExpectedValue,
                        Message = a.Message
                    }).ToList(),
                    ExtractedVariables = r.ExtractedVariables,
                    ExecutedAt = r.ExecutedAt
                }).ToList();

                _logger.LogInformation(
                    "Fetched {Count}/{Total} results for batch {BatchId}",
                    dtos.Count, total, batchId);

                return (dtos, total);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching results for batch {BatchId}", batchId);
                throw;
            }
        }

        // ── GET HISTORY ───────────────────────────────────────────────────────
        public async Task<(IEnumerable<ExecutionBatchDto> Batches, int TotalCount)> GetHistoryAsync(
            Guid suiteId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching execution history for suite {SuiteId}. Page={Page}",
                    suiteId, pageNumber);

                var (batches, total) = await _executionRepository
                    .GetBatchHistoryAsync(suiteId, pageNumber, pageSize, cancellationToken);

                var dtos = batches.Select(b => new ExecutionBatchDto
                {
                    BatchId = b.Id,
                    SuiteId = b.TestSuiteId,
                    ProjectId = b.ProjectId,
                    Status = b.Status.ToString(),
                    TotalSteps = b.TotalSteps,
                    PassedSteps = b.PassedSteps,
                    FailedSteps = b.FailedSteps,
                    SkippedSteps = b.SkippedSteps,
                    ErrorSteps = b.ErrorSteps,
                    PassPercentage = b.PassPercentage,
                    TotalDurationMs = b.TotalDurationMs,
                    StartedAt = b.StartedAt,
                    CompletedAt = b.CompletedAt
                }).ToList();

                _logger.LogInformation(
                    "Fetched {Count}/{Total} execution batches for suite {SuiteId}",
                    dtos.Count, total, suiteId);

                return (dtos, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching execution history for suite {SuiteId}", suiteId);
                throw;
            }
        }
    }
}