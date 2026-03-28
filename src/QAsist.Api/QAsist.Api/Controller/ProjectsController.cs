using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;

namespace QAsist.Api.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectService projectService,
            ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        // ── GET /api/v1/projects/{id} ─────────────────────────────────────────
        [HttpGet("{id:guid}", Name = "GetProjectById")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetByIdAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Projects] GetById started. Id={Id} User={UserId}",
                id, User.GetUserId());
            try
            {
                var project = await _projectService.GetByIdAsync(id, cancellationToken);
                var response = ApiResponse<ProjectDto>.SuccessResponse(project);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Projects] GetById succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Projects] GetById failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug(
                    "[Projects] GetById completed. Id={Id}", id);
            }
        }

        // ── GET /api/v1/projects ──────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Projects] GetAll started. User={UserId}", User.GetUserId());
            try
            {
                var projects = await _projectService.GetAllAsync(cancellationToken);
                var response = ApiResponse<IEnumerable<ProjectDto>>.SuccessResponse(projects);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Projects] GetAll succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Projects] GetAll failed.");
                throw;
            }
            finally
            {
                _logger.LogDebug("[Projects] GetAll completed.");
            }
        }

        // ── GET /api/v1/projects/paged ────────────────────────────────────────
        [HttpGet("paged")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<ProjectDto>>>> GetPagedAsync(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[Projects] GetPaged started. Page={Page} Size={Size}",
                pageNumber, pageSize);
            try
            {
                var (projects, totalCount) = await _projectService
                    .GetPagedAsync(pageNumber, pageSize, cancellationToken);

                var response = PagedApiResponse<IEnumerable<ProjectDto>>
                    .SuccessResponse(projects, totalCount, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Projects] GetPaged succeeded. Total={Total}", totalCount);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Projects] GetPaged failed.");
                throw;
            }
            finally
            {
                _logger.LogDebug("[Projects] GetPaged completed.");
            }
        }

        // ── POST /api/v1/projects ─────────────────────────────────────────────
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateAsync(
            [FromBody] CreateProjectDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Projects] Create started. Name={Name} User={UserId}",
                dto.Name, userId);
            try
            {
                var project = await _projectService.CreateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<ProjectDto>.SuccessResponse(
                    project, ResponseMessages.ProjectCreatedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Projects] Create succeeded. ProjectId={Id}", project.Id);
                return CreatedAtRoute("GetProjectById", new { id = project.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Projects] Create failed. Name={Name}", dto.Name);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Projects] Create completed.");
            }
        }

        // ── PUT /api/v1/projects/{id} ─────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateProjectDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Projects] Update started. Id={Id} User={UserId}", id, userId);
            try
            {
                var project = await _projectService.UpdateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<ProjectDto>.SuccessResponse(
                    project, ResponseMessages.ProjectUpdatedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Projects] Update succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Projects] Update failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Projects] Update completed. Id={Id}", id);
            }
        }

        // ── DELETE /api/v1/projects/{id} ──────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Projects] Delete started. Id={Id} User={UserId}", id, userId);
            try
            {
                await _projectService.DeleteAsync(id, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, ResponseMessages.ProjectDeletedSuccessfully);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Projects] Delete succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Projects] Delete failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Projects] Delete completed. Id={Id}", id);
            }
        }
    }
}