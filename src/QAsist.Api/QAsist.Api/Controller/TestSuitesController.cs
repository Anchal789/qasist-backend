using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        // ── GET /api/testsuites/{id} ──────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TestSuiteDto>>> GetByIdAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var suite = await _suiteService.GetByIdAsync(id, cancellationToken);
            var response = ApiResponse<TestSuiteDto>.SuccessResponse(suite);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── GET /api/testsuites/{id}/details ─────────────────────────────────
        /// <summary>Returns suite with full tree: cases → steps → assertions + extractions</summary>
        [HttpGet("{id:guid}/details")]
        public async Task<ActionResult<ApiResponse<TestSuiteDetailDto>>> GetWithDetailsAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var suite = await _suiteService.GetWithDetailsAsync(id, cancellationToken);
            var response = ApiResponse<TestSuiteDetailDto>.SuccessResponse(suite);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── GET /api/testsuites/project/{projectId} ───────────────────────────
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TestSuiteDto>>>> GetByProjectAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            var suites = await _suiteService.GetByProjectAsync(projectId, cancellationToken);
            var response = ApiResponse<IEnumerable<TestSuiteDto>>.SuccessResponse(suites);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── GET /api/testsuites/project/{projectId}/paged ────────────────────
        [HttpGet("project/{projectId:guid}/paged")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<TestSuiteDto>>>> GetPagedAsync(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var (suites, total) = await _suiteService.GetPagedAsync(
                projectId, pageNumber, pageSize, cancellationToken);

            var response = PagedApiResponse<IEnumerable<TestSuiteDto>>
                .SuccessResponse(suites, total, pageNumber, pageSize);

            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── POST /api/testsuites ──────────────────────────────────────────────
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<TestSuiteDto>>> CreateAsync(
            [FromBody] CreateTestSuiteDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var suite = await _suiteService.CreateAsync(dto, userId, cancellationToken);
            var response = ApiResponse<TestSuiteDto>.SuccessResponse(
                suite, ResponseMessages.TestSuiteCreatedSuccessfully);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── PUT /api/testsuites/{id} ──────────────────────────────────────────
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
            var suite = await _suiteService.UpdateAsync(dto, userId, cancellationToken);
            var response = ApiResponse<TestSuiteDto>.SuccessResponse(
                suite, ResponseMessages.TestSuiteUpdatedSuccessfully);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── DELETE /api/testsuites/{id} ───────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _suiteService.DeleteAsync(id, userId, cancellationToken);
            var response = ApiResponse<object>.SuccessResponse(
                null, ResponseMessages.TestSuiteDeletedSuccessfully);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }

        // ── POST /api/testsuites/{id}/execute ─────────────────────────────────
        /// <summary>Execute the suite — returns batchId for polling results.</summary>
        [HttpPost("{id:guid}/execute")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QaEngineer)]
        public async Task<ActionResult<ApiResponse<ExecuteSuiteResponseDto>>> ExecuteAsync(
            Guid id,
            [FromBody] ExecuteSuiteRequestDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _suiteService.ExecuteAsync(id, dto, userId, cancellationToken);
            var response = ApiResponse<ExecuteSuiteResponseDto>.SuccessResponse(result);
            response.CorrelationId = HttpContext.TraceIdentifier;
            return Ok(response);
        }
    }
}
