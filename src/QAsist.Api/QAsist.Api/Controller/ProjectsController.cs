using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;

namespace QAsist.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var project = await _projectService.GetByIdAsync(id, cancellationToken);
            var response = ApiResponse<ProjectDto>.SuccessResponse(project);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetAllAsync(cancellationToken);
            var response = ApiResponse<IEnumerable<ProjectDto>>.SuccessResponse(projects);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<ProjectDto>>>> GetPagedAsync(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var (projects, totalCount) = await _projectService.GetPagedAsync(
                pageNumber,
                pageSize,
                cancellationToken);

            var response = PagedApiResponse<IEnumerable<ProjectDto>>.SuccessResponse(
                projects,
                totalCount,
                pageNumber,
                pageSize);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateAsync(
            [FromBody] CreateProjectDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var project = await _projectService.CreateAsync(dto, userId, cancellationToken);

            var response = ApiResponse<ProjectDto>.SuccessResponse(
                project,
                ResponseMessages.ProjectCreatedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = project.Id },
                response);
        }

        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateProjectDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            var project = await _projectService.UpdateAsync(dto, userId, cancellationToken);

            var response = ApiResponse<ProjectDto>.SuccessResponse(
                project,
                ResponseMessages.ProjectUpdatedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _projectService.DeleteAsync(id, userId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                ResponseMessages.ProjectDeletedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }
    }
}
