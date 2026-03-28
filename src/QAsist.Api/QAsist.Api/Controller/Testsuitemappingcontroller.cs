using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;
using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Api.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/TestSuiteMappings")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class TestSuiteMappingController : ControllerBase
    {
        private readonly ITestSuiteMappingService _mappingService;
        private readonly ISuiteExecutionService _executionService;
        private readonly ILogger<TestSuiteMappingController> _logger;

        public TestSuiteMappingController(
            ITestSuiteMappingService mappingService,
            ISuiteExecutionService executionService,
            ILogger<TestSuiteMappingController> logger)
        {
            _mappingService = mappingService;
            _executionService = executionService;
            _logger = logger;
        }

        [HttpGet("{id:guid}/testcases")]
        public async Task<ActionResult<ApiResponse<IEnumerable<SuiteTestCaseMappingDto>>>> GetTestCasesAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SuiteMapping] GetTestCases started. SuiteId={Id}", id);
            try
            {
                var mappings = await _mappingService
                    .GetMappedTestCasesAsync(id, cancellationToken);
                var response = ApiResponse<IEnumerable<SuiteTestCaseMappingDto>>
                    .SuccessResponse(mappings);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[SuiteMapping] GetTestCases succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SuiteMapping] GetTestCases failed. SuiteId={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[SuiteMapping] GetTestCases completed. SuiteId={Id}", id);
            }
        }

        [HttpPost("{id:guid}/testcases")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<AddTestCasesResultDto>>> AddTestCasesAsync(
            Guid id,
            [FromBody] AddTestCasesToSuiteDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[SuiteMapping] AddTestCases started. SuiteId={Id} Count={Count} User={UserId}",
                id, dto.TestCases.Count, userId);
            try
            {
                var result = await _mappingService.AddTestCasesAsync(
                    id, dto, userId, cancellationToken);
                var response = ApiResponse<AddTestCasesResultDto>.SuccessResponse(
                    result,
                    $"{result.AddedCount} test case(s) added." +
                    (result.SkippedCount > 0
                        ? $" {result.SkippedCount} duplicate(s) skipped." : ""));
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[SuiteMapping] AddTestCases succeeded. Added={Added}", result.AddedCount);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SuiteMapping] AddTestCases failed. SuiteId={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[SuiteMapping] AddTestCases completed. SuiteId={Id}", id);
            }
        }

        [HttpDelete("{id:guid}/testcases/{mappingId:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<object>>> RemoveTestCaseAsync(
            Guid id, Guid mappingId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[SuiteMapping] Remove started. SuiteId={Id} MappingId={MId}",
                id, mappingId);
            try
            {
                await _mappingService.RemoveMappingAsync(id, mappingId, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, "Test case removed from suite.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[SuiteMapping] Remove succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SuiteMapping] Remove failed. MappingId={MId}", mappingId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[SuiteMapping] Remove completed.");
            }
        }

        [HttpPatch("{id:guid}/testcases/reorder")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<object>>> ReorderAsync(
            Guid id,
            [FromBody] ReorderSuiteTestCasesDto dto,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SuiteMapping] Reorder started. SuiteId={Id}", id);
            try
            {
                await _mappingService.ReorderAsync(id, dto, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, "Test cases reordered successfully.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[SuiteMapping] Reorder succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SuiteMapping] Reorder failed. SuiteId={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[SuiteMapping] Reorder completed. SuiteId={Id}", id);
            }
        }

        [HttpPost("{id:guid}/execute")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<SuiteExecutionBatchDto>>> ExecuteAsync(
            Guid id,
            [FromBody] ExecuteSuiteDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[SuiteMapping] Execute started. SuiteId={Id} User={UserId}", id, userId);
            try
            {
                var result = await _executionService.ExecuteAsync(
                    id, dto, userId, cancellationToken);
                var response = ApiResponse<SuiteExecutionBatchDto>.SuccessResponse(
                    result, "Execution started. Use batchId to poll results.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[SuiteMapping] Execute triggered. BatchId={BatchId}", result.BatchId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SuiteMapping] Execute failed. SuiteId={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[SuiteMapping] Execute completed. SuiteId={Id}", id);
            }
        }

        [HttpGet("{id:guid}/executions")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<SuiteExecutionBatchDto>>>> GetHistoryAsync(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[SuiteMapping] GetHistory started. SuiteId={Id}", id);
            try
            {
                var (batches, total) = await _executionService
                    .GetHistoryAsync(id, pageNumber, pageSize, cancellationToken);
                var response = PagedApiResponse<IEnumerable<SuiteExecutionBatchDto>>
                    .SuccessResponse(batches, total, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[SuiteMapping] GetHistory succeeded. Total={Total}", total);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SuiteMapping] GetHistory failed. SuiteId={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[SuiteMapping] GetHistory completed. SuiteId={Id}", id);
            }
        }
    }
}