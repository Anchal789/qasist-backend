using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;

namespace QAsist.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class TestExecutionController : ControllerBase
    {
        private readonly ITestExecutionService _executionService;
        private readonly ILogger<TestExecutionController> _logger;

        public TestExecutionController(
            ITestExecutionService executionService,
            ILogger<TestExecutionController> logger)
        {
            _executionService = executionService;
            _logger = logger;
        }

        [HttpPost("execute")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QALead, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestExecutionSummaryDto>>> ExecuteTestCasesAsync(
            [FromBody] ExecuteTestCasesRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestExecution] Execute started. ProjectId={ProjectId} Count={Count} User={UserId}",
                request.ProjectId, request.TestCases.Count, userId);
            try
            {
                var result = await _executionService
                    .ExecuteTestCasesAsync(request, userId, cancellationToken);

                var message = result.PassedTests == result.TotalTests
                    ? $"All {result.TotalTests} tests passed"
                    : $"{result.PassedTests}/{result.TotalTests} tests passed ({result.PassPercentage}%)";

                var response = ApiResponse<TestExecutionSummaryDto>
                    .SuccessResponse(result, message);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[TestExecution] Execute succeeded. Pass={Pass}/{Total}",
                    result.PassedTests, result.TotalTests);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestExecution] Execute failed. ProjectId={Id}", request.ProjectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestExecution] Execute completed.");
            }
        }

        [HttpGet("history/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestExecutionSummaryDto>>>> GetExecutionHistoryAsync(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[TestExecution] GetHistory started. ProjectId={Id}", projectId);
            try
            {
                var history = await _executionService
                    .GetExecutionHistoryAsync(projectId, pageNumber, pageSize, cancellationToken);
                var response = ApiResponse<IEnumerable<TestExecutionSummaryDto>>
                    .SuccessResponse(history);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestExecution] GetHistory succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestExecution] GetHistory failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestExecution] GetHistory completed.");
            }
        }

        [HttpGet("details/{executionBatchId:guid}")]
        public async Task<ActionResult<ApiResponse<TestExecutionSummaryDto>>> GetExecutionDetailsAsync(
            Guid executionBatchId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestExecution] GetDetails started. BatchId={Id}", executionBatchId);
            try
            {
                var details = await _executionService
                    .GetExecutionDetailsAsync(executionBatchId, cancellationToken);

                if (details is null)
                {
                    _logger.LogWarning(
                        "[TestExecution] Batch not found. BatchId={Id}", executionBatchId);
                    return NotFound(ApiResponse<TestExecutionSummaryDto>.ErrorResponse(
                        $"Execution batch {executionBatchId} not found"));
                }

                var response = ApiResponse<TestExecutionSummaryDto>.SuccessResponse(details);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestExecution] GetDetails succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestExecution] GetDetails failed. BatchId={Id}", executionBatchId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestExecution] GetDetails completed.");
            }
        }
    }
}

