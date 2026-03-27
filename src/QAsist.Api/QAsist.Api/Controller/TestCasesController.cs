using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Route("api/[controller]")]
    [Authorize]
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

        // ── GET /api/testcases/{id} ───────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TestCaseDto>>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var testCase = await _testCaseService.GetByIdAsync(id, cancellationToken);

            var response = ApiResponse<TestCaseDto>.SuccessResponse(testCase);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/project/{projectId} ───────────────────────────────
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var testCases = await _testCaseService.GetByProjectAsync(projectId, cancellationToken);

            var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>.SuccessResponse(testCases);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/project/{projectId}/paged ─────────────────────────
        [HttpGet("project/{projectId:guid}/paged")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetPagedAsync(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var (testCases, totalCount) = await _testCaseService.GetPagedAsync(
                projectId, pageNumber, pageSize, cancellationToken);

            var response = PagedApiResponse<IEnumerable<TestCaseSummaryDto>>.SuccessResponse(
                testCases, totalCount, pageNumber, pageSize);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/project/{projectId}/status/{status} ───────────────
        [HttpGet("project/{projectId:guid}/status/{status}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetByStatusAsync(
            Guid projectId,
            TestCaseStatus status,
            CancellationToken cancellationToken)
        {
            var testCases = await _testCaseService.GetByStatusAsync(projectId, status, cancellationToken);

            var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>.SuccessResponse(testCases);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/project/{projectId}/ai-generated ──────────────────
        [HttpGet("project/{projectId:guid}/ai-generated")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetAiGeneratedAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var testCases = await _testCaseService.GetAiGeneratedAsync(projectId, cancellationToken);

            var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>.SuccessResponse(testCases);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/assignee/{userId} ─────────────────────────────────
        [HttpGet("assignee/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> GetByAssigneeAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var testCases = await _testCaseService.GetByAssigneeAsync(userId, cancellationToken);

            var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>.SuccessResponse(testCases);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/project/{projectId}/search ────────────────────────
        [HttpGet("project/{projectId:guid}/search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestCaseSummaryDto>>>> SearchAsync(
            Guid projectId,
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var badRequest = ApiResponse<IEnumerable<TestCaseSummaryDto>>
                    .ErrorResponse("Search term cannot be empty.");
                return BadRequest(badRequest);
            }

            var testCases = await _testCaseService.SearchAsync(projectId, searchTerm, cancellationToken);

            var response = ApiResponse<IEnumerable<TestCaseSummaryDto>>.SuccessResponse(testCases);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── GET /api/testcases/project/{projectId}/statistics ────────────────────
        [HttpGet("project/{projectId:guid}/statistics")]
        public async Task<ActionResult<ApiResponse<TestCaseStatisticsDto>>> GetStatisticsAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var stats = await _testCaseService.GetStatisticsAsync(projectId, cancellationToken);

            var response = ApiResponse<TestCaseStatisticsDto>.SuccessResponse(stats);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── POST /api/testcases ───────────────────────────────────────────────────
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestCaseDto>>> CreateAsync(
            [FromBody] CreateTestCaseDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var testCase = await _testCaseService.CreateAsync(dto, userId, cancellationToken);

            var response = ApiResponse<TestCaseDto>.SuccessResponse(
                testCase,
                ResponseMessages.TestCaseCreatedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── PUT /api/testcases/{id} ───────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestCaseDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateTestCaseDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            var testCase = await _testCaseService.UpdateAsync(dto, userId, cancellationToken);

            var response = ApiResponse<TestCaseDto>.SuccessResponse(
                testCase,
                ResponseMessages.TestCaseUpdatedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── PATCH /api/testcases/{id}/status ─────────────────────────────────────
        [HttpPatch("{id:guid}/status")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatusAsync(
            Guid id,
            [FromBody] UpdateTestCaseStatusDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _testCaseService.UpdateStatusAsync(id, dto, userId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                ResponseMessages.TestCaseStatusUpdated);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── PATCH /api/testcases/{id}/assign ─────────────────────────────────────
        [HttpPatch("{id:guid}/assign")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> AssignAsync(
            Guid id,
            [FromBody] AssignTestCaseDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _testCaseService.AssignAsync(id, dto, userId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                ResponseMessages.TestCaseAssignedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        // ── DELETE /api/testcases/{id} ────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _testCaseService.DeleteAsync(id, userId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                ResponseMessages.TestCaseDeletedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }
    }
}