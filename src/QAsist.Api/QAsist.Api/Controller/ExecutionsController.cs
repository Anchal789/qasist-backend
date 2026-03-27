using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IServices;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        // ── GET /api/executions/{batchId} ─────────────────────────────────────
        /// <summary>Get execution batch summary (poll after execute).</summary>
        [HttpGet("{batchId:guid}")]
        public async Task<ActionResult<ApiResponse<ExecutionBatchDto>>> GetBatchAsync(
            Guid batchId, CancellationToken cancellationToken)
        {
            var batch = await _executionService.GetBatchAsync(batchId, cancellationToken);
            var response = ApiResponse<ExecutionBatchDto>.SuccessResponse(batch);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── GET /api/executions/{batchId}/results ─────────────────────────────
        /// <summary>Get paged step results for a batch.</summary>
        [HttpGet("{batchId:guid}/results")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<StepResultDto>>>> GetResultsAsync(
            Guid batchId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var (results, total) = await _executionService.GetResultsAsync(
                batchId, pageNumber, pageSize, cancellationToken);

            var response = PagedApiResponse<IEnumerable<StepResultDto>>
                .SuccessResponse(results, total, pageNumber, pageSize);

            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── GET /api/executions/history/{suiteId} ─────────────────────────────
        /// <summary>Get past execution batches for a suite.</summary>
        [HttpGet("history/{suiteId:guid}")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<ExecutionBatchDto>>>> GetHistoryAsync(
            Guid suiteId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var (batches, total) = await _executionService.GetHistoryAsync(
                suiteId, pageNumber, pageSize, cancellationToken);

            var response = PagedApiResponse<IEnumerable<ExecutionBatchDto>>
                .SuccessResponse(batches, total, pageNumber, pageSize);

            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }
    }
}
