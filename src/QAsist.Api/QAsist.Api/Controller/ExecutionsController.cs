using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IServices;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Api.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class ExecutionsController : ControllerBase
    {
        private readonly IExecutionService _executionService;
        private readonly ILogger<ExecutionsController> _logger;

        public ExecutionsController(
            IExecutionService executionService,
            ILogger<ExecutionsController> logger)
        {
            _executionService = executionService;
            _logger = logger;
        }

        [HttpGet("{batchId:guid}")]
        public async Task<ActionResult<ApiResponse<ExecutionBatchDto>>> GetBatchAsync(
            Guid batchId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Executions] GetBatch started. BatchId={Id}", batchId);
            try
            {
                var batch = await _executionService.GetBatchAsync(batchId, cancellationToken);
                var response = ApiResponse<ExecutionBatchDto>.SuccessResponse(batch);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Executions] GetBatch succeeded. BatchId={Id} Status={Status}",
                    batchId, batch.Status);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Executions] GetBatch failed. BatchId={Id}", batchId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Executions] GetBatch completed. BatchId={Id}", batchId);
            }
        }

        [HttpGet("{batchId:guid}/results")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<ExecutionStepResultDto>>>> GetResultsAsync(
            Guid batchId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[Executions] GetResults started. BatchId={Id} Page={Page}",
                batchId, pageNumber);
            try
            {
                var (results, total) = await _executionService
                    .GetResultsAsync(batchId, pageNumber, pageSize, cancellationToken);
                var response = PagedApiResponse<IEnumerable<ExecutionStepResultDto>>
                    .SuccessResponse(results, total, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Executions] GetResults succeeded. Total={Total}", total);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Executions] GetResults failed. BatchId={Id}", batchId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Executions] GetResults completed. BatchId={Id}", batchId);
            }
        }

        [HttpGet("history/{suiteId:guid}")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<ExecutionBatchDto>>>> GetHistoryAsync(
            Guid suiteId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[Executions] GetHistory started. SuiteId={Id}", suiteId);
            try
            {
                var (batches, total) = await _executionService
                    .GetHistoryAsync(suiteId, pageNumber, pageSize, cancellationToken);
                var response = PagedApiResponse<IEnumerable<ExecutionBatchDto>>
                    .SuccessResponse(batches, total, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Executions] GetHistory succeeded. Total={Total}", total);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Executions] GetHistory failed. SuiteId={Id}", suiteId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Executions] GetHistory completed. SuiteId={Id}", suiteId);
            }
        }
    }
}