using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Api.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class TestSuitesController : ControllerBase
    {
        private readonly ITestSuiteService _suiteService;
        private readonly ILogger<TestSuitesController> _logger;

        public TestSuitesController(
            ITestSuiteService suiteService,
            ILogger<TestSuitesController> logger)
        {
            _suiteService = suiteService;
            _logger = logger;
        }

        // ── GET /api/v1/testsuites/{id} ───────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TestSuiteDto>>> GetByIdAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestSuites] GetById started. Id={Id}", id);
            try
            {
                var suite = await _suiteService.GetByIdAsync(id, cancellationToken);
                var response = ApiResponse<TestSuiteDto>.SuccessResponse(suite);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestSuites] GetById succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestSuites] GetById failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] GetById completed. Id={Id}", id);
            }
        }

        // ── GET /api/v1/testsuites/{id}/details ───────────────────────────────
        [HttpGet("{id:guid}/details")]
        public async Task<ActionResult<ApiResponse<TestSuiteDetailDto>>> GetWithDetailsAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestSuites] GetWithDetails started. Id={Id}", id);
            try
            {
                var suite = await _suiteService.GetWithDetailsAsync(id, cancellationToken);
                var response = ApiResponse<TestSuiteDetailDto>.SuccessResponse(suite);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestSuites] GetWithDetails succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestSuites] GetWithDetails failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] GetWithDetails completed. Id={Id}", id);
            }
        }

        // ── GET /api/v1/testsuites/project/{projectId} ────────────────────────
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestSuiteDto>>>> GetByProjectAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestSuites] GetByProject started. ProjectId={Id}", projectId);
            try
            {
                var suites = await _suiteService.GetByProjectAsync(projectId, cancellationToken);
                var response = ApiResponse<IEnumerable<TestSuiteDto>>.SuccessResponse(suites);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestSuites] GetByProject succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestSuites] GetByProject failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] GetByProject completed.");
            }
        }

        // ── GET /api/v1/testsuites/project/{projectId}/paged ──────────────────
        [HttpGet("project/{projectId:guid}/paged")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<TestSuiteDto>>>> GetPagedAsync(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[TestSuites] GetPaged started. ProjectId={Id} Page={Page}",
                projectId, pageNumber);
            try
            {
                var (suites, total) = await _suiteService
                    .GetPagedAsync(projectId, pageNumber, pageSize, cancellationToken);
                var response = PagedApiResponse<IEnumerable<TestSuiteDto>>
                    .SuccessResponse(suites, total, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[TestSuites] GetPaged succeeded. Total={Total}", total);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestSuites] GetPaged failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] GetPaged completed.");
            }
        }

        // ── POST /api/v1/testsuites ───────────────────────────────────────────
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestSuiteDto>>> CreateAsync(
            [FromBody] CreateTestSuiteDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestSuites] Create started. Name={Name} User={UserId}",
                dto.Name, userId);
            try
            {
                var suite = await _suiteService.CreateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<TestSuiteDto>.SuccessResponse(
                    suite, ResponseMessages.TestSuiteCreatedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[TestSuites] Create succeeded. Id={Id}", suite.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestSuites] Create failed. Name={Name}", dto.Name);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] Create completed.");
            }
        }

        // ── PUT /api/v1/testsuites/{id} ───────────────────────────────────────
        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestSuiteDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateTestSuiteDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestSuites] Update started. Id={Id} User={UserId}", id, userId);
            try
            {
                var suite = await _suiteService.UpdateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<TestSuiteDto>.SuccessResponse(
                    suite, ResponseMessages.TestSuiteUpdatedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestSuites] Update succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestSuites] Update failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] Update completed. Id={Id}", id);
            }
        }

        // ── DELETE /api/v1/testsuites/{id} ────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestSuites] Delete started. Id={Id} User={UserId}", id, userId);
            try
            {
                await _suiteService.DeleteAsync(id, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, ResponseMessages.TestSuiteDeletedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestSuites] Delete succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestSuites] Delete failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] Delete completed. Id={Id}", id);
            }
        }

        // ── POST /api/v1/testsuites/{id}/execute ──────────────────────────────
        [HttpPost("{id:guid}/execute")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<ExecuteSuiteResponseDto>>> ExecuteAsync(
            Guid id,
            [FromBody] ExecuteSuiteRequestDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestSuites] Execute started. SuiteId={Id} User={UserId}", id, userId);
            try
            {
                var result = await _suiteService.ExecuteAsync(id, dto, userId, cancellationToken);
                var response = ApiResponse<ExecuteSuiteResponseDto>.SuccessResponse(result);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[TestSuites] Execute triggered. BatchId={BatchId}", result.BatchId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestSuites] Execute failed. SuiteId={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestSuites] Execute completed. SuiteId={Id}", id);
            }
        }
    }
}