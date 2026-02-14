using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;

namespace QAsist.Api.Controllers
{
    /// <summary>
    /// API Test Execution Engine - Replaces Postman/REST Assured
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        /// <summary>
        /// Execute a batch of API test cases
        /// </summary>
        [HttpPost("execute")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.QALead, UserRole.QAEngineer)]
        public async Task<ActionResult<ApiResponse<TestExecutionSummaryDto>>> ExecuteTestCasesAsync(
            [FromBody] ExecuteTestCasesRequestDto request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "User {UserId} executing {Count} test cases for Project {ProjectId} in {Environment}",
                User.GetUserId(),
                request.TestCases.Count,
                request.ProjectId,
                request.Environment);

            var userId = User.GetUserId();
            var result = await _executionService.ExecuteTestCasesAsync(request, userId, cancellationToken);

            var message = result.PassedTests == result.TotalTests
                ? $"All {result.TotalTests} tests passed successfully"
                : $"{result.PassedTests}/{result.TotalTests} tests passed ({result.PassPercentage}%)";

            var response = ApiResponse<TestExecutionSummaryDto>.SuccessResponse(result, message);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        /// <summary>
        /// Get test execution history for a project
        /// </summary>
        [HttpGet("history/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestExecutionSummaryDto>>>> GetExecutionHistoryAsync(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "User {UserId} retrieving execution history for Project {ProjectId}",
                User.GetUserId(),
                projectId);

            var history = await _executionService.GetExecutionHistoryAsync(
                projectId,
                pageNumber,
                pageSize,
                cancellationToken);

            var response = ApiResponse<IEnumerable<TestExecutionSummaryDto>>.SuccessResponse(history);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        /// <summary>
        /// Get detailed results for a specific execution batch
        /// </summary>
        [HttpGet("details/{executionBatchId:guid}")]
        public async Task<ActionResult<ApiResponse<TestExecutionSummaryDto>>> GetExecutionDetailsAsync(
            Guid executionBatchId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "User {UserId} retrieving execution details for batch {BatchId}",
                User.GetUserId(),
                executionBatchId);

            var details = await _executionService.GetExecutionDetailsAsync(executionBatchId, cancellationToken);

            if (details == null)
            {
                var errorResponse = ApiResponse<TestExecutionSummaryDto>.ErrorResponse(
                    $"Execution batch {executionBatchId} not found");
                errorResponse.CorrelationId = HttpContext.TraceIdentifier;
                return NotFound(errorResponse);
            }

            var response = ApiResponse<TestExecutionSummaryDto>.SuccessResponse(details);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }
    }
}