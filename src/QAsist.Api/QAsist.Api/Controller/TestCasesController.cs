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

namespace QAsist.Api.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class TestCasesController : ControllerBase
    {
        private readonly ITestCaseService _testCaseService;
        private readonly ILogger<TestCasesController> _logger;

        public TestCasesController(
            ITestCaseService testCaseService,
            ILogger<TestCasesController> logger)
        {
            _testCaseService = testCaseService;
            _logger = logger;
        }

        // ── GET /api/v1/testcases/{id} ────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TestCaseDto>>> GetByIdAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] GetById started. Id={Id}", id);
            try
            {
                var testCase = await _testCaseService.GetByIdAsync(id, cancellationToken);
                var response = ApiResponse<TestCaseDto>.SuccessResponse(testCase);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] GetById succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestCases] GetById failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] GetById completed. Id={Id}", id);
            }
        }

        // ── GET /api/v1/testcases/project/{projectId} ─────────────────────────
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetByProjectAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] GetByProject started. ProjectId={Id}", projectId);
            try
            {
                var testCases = await _testCaseService
                    .GetByProjectAsync(projectId, cancellationToken);
                var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .SuccessResponse(testCases);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] GetByProject succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] GetByProject failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug(
                    "[TestCases] GetByProject completed. ProjectId={Id}", projectId);
            }
        }

        // ── GET /api/v1/testcases/project/{projectId}/paged ───────────────────
        [HttpGet("project/{projectId:guid}/paged")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetPagedAsync(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[TestCases] GetPaged started. ProjectId={Id} Page={Page}",
                projectId, pageNumber);
            try
            {
                var (testCases, total) = await _testCaseService
                    .GetPagedAsync(projectId, pageNumber, pageSize, cancellationToken);
                var response = PagedApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .SuccessResponse(testCases, total, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[TestCases] GetPaged succeeded. Total={Total}", total);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] GetPaged failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug(
                    "[TestCases] GetPaged completed. ProjectId={Id}", projectId);
            }
        }

        // ── GET /api/v1/testcases/project/{projectId}/status/{status} ─────────
        [HttpGet("project/{projectId:guid}/status/{status}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetByStatusAsync(
            Guid projectId, TestCaseStatus status, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] GetByStatus started. ProjectId={Id} Status={Status}",
                projectId, status);
            try
            {
                var testCases = await _testCaseService
                    .GetByStatusAsync(projectId, status, cancellationToken);
                var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .SuccessResponse(testCases);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] GetByStatus succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] GetByStatus failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] GetByStatus completed.");
            }
        }

        // ── GET /api/v1/testcases/project/{projectId}/ai-generated ────────────
        [HttpGet("project/{projectId:guid}/ai-generated")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetAiGeneratedAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] GetAiGenerated started. ProjectId={Id}", projectId);
            try
            {
                var testCases = await _testCaseService
                    .GetAiGeneratedAsync(projectId, cancellationToken);
                var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .SuccessResponse(testCases);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] GetAiGenerated succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] GetAiGenerated failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] GetAiGenerated completed.");
            }
        }

        // ── GET /api/v1/testcases/assignee/{userId} ───────────────────────────
        [HttpGet("assignee/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetByAssigneeAsync(
            Guid userId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] GetByAssignee started. UserId={Id}", userId);
            try
            {
                var testCases = await _testCaseService
                    .GetByAssigneeAsync(userId, cancellationToken);
                var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .SuccessResponse(testCases);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] GetByAssignee succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] GetByAssignee failed. UserId={Id}", userId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] GetByAssignee completed.");
            }
        }

        // ── GET /api/v1/testcases/project/{projectId}/search ──────────────────
        [HttpGet("project/{projectId:guid}/search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> SearchAsync(
            Guid projectId,
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] Search started. ProjectId={Id} Term={Term}",
                projectId, searchTerm);
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest(ApiResponse<IEnumerable<TestCaseSummaryDto>>
                        .ErrorResponse("Search term cannot be empty."));

                var testCases = await _testCaseService
                    .SearchAsync(projectId, searchTerm, cancellationToken);
                var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .SuccessResponse(testCases);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] Search succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] Search failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] Search completed.");
            }
        }

        // ── GET /api/v1/testcases/project/{projectId}/statistics ──────────────
        [HttpGet("project/{projectId:guid}/statistics")]
        public async Task<ActionResult<ApiResponse<TestCaseStatisticsDto>>> GetStatisticsAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[TestCases] GetStatistics started. ProjectId={Id}", projectId);
            try
            {
                var stats = await _testCaseService.GetStatisticsAsync(projectId, cancellationToken);
                var response = ApiResponse<TestCaseStatisticsDto>.SuccessResponse(stats);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] GetStatistics succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] GetStatistics failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] GetStatistics completed.");
            }
        }

        // ── POST /api/v1/testcases ────────────────────────────────────────────
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestCaseDto>>> CreateAsync(
            [FromBody] CreateTestCaseDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestCases] Create started. Title={Title} User={UserId}",
                dto.Title, userId);
            try
            {
                var testCase = await _testCaseService.CreateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<TestCaseDto>.SuccessResponse(
                    testCase, ResponseMessages.TestCaseCreatedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[TestCases] Create succeeded. Id={Id}", testCase.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[TestCases] Create failed. Title={Title}", dto.Title);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] Create completed.");
            }
        }

        // ── PUT /api/v1/testcases/{id} ────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestCaseDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateTestCaseDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestCases] Update started. Id={Id} User={UserId}", id, userId);
            try
            {
                var testCase = await _testCaseService.UpdateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<TestCaseDto>.SuccessResponse(
                    testCase, ResponseMessages.TestCaseUpdatedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] Update succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestCases] Update failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] Update completed. Id={Id}", id);
            }
        }

        // ── PATCH /api/v1/testcases/{id}/status ───────────────────────────────
        [HttpPatch("{id:guid}/status")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatusAsync(
            Guid id,
            [FromBody] UpdateTestCaseStatusDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestCases] UpdateStatus started. Id={Id} Status={Status}",
                id, dto.Status);
            try
            {
                await _testCaseService.UpdateStatusAsync(id, dto, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, ResponseMessages.TestCaseStatusUpdated);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] UpdateStatus succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestCases] UpdateStatus failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] UpdateStatus completed. Id={Id}", id);
            }
        }

        // ── PATCH /api/v1/testcases/{id}/assign ───────────────────────────────
        [HttpPatch("{id:guid}/assign")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> AssignAsync(
            Guid id,
            [FromBody] AssignTestCaseDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestCases] Assign started. Id={Id} AssignTo={AssignTo}",
                id, dto.AssignedTo);
            try
            {
                await _testCaseService.AssignAsync(id, dto, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, ResponseMessages.TestCaseAssignedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] Assign succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestCases] Assign failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] Assign completed. Id={Id}", id);
            }
        }

        // ── DELETE /api/v1/testcases/{id} ─────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[TestCases] Delete started. Id={Id} User={UserId}", id, userId);
            try
            {
                await _testCaseService.DeleteAsync(id, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, ResponseMessages.TestCaseDeletedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[TestCases] Delete succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TestCases] Delete failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[TestCases] Delete completed. Id={Id}", id);
            }
        }
    }
}