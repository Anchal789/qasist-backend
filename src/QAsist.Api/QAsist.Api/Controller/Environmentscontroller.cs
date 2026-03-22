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
    /// Environment Configuration Management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnvironmentsController : ControllerBase
    {
        private readonly IEnvironmentService _environmentService;
        private readonly ILogger<EnvironmentsController> _logger;

        public EnvironmentsController(
            IEnvironmentService environmentService,
            ILogger<EnvironmentsController> logger)
        {
            _environmentService = environmentService;
            _logger = logger;
        }

        /// <summary>
        /// Get all environments for a project
        /// </summary>
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<EnvironmentDto>>>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var environments = await _environmentService.GetByProjectAsync(projectId, cancellationToken);

            var response = ApiResponse<IEnumerable<EnvironmentDto>>.SuccessResponse(environments);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        /// <summary>
        /// Get environment by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<EnvironmentDto>>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var environment = await _environmentService.GetByIdAsync(id, cancellationToken);

            var response = ApiResponse<EnvironmentDto>.SuccessResponse(environment);
            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        /// <summary>
        /// Create new environment
        /// </summary>
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<EnvironmentDto>>> CreateAsync(
            [FromBody] CreateEnvironmentDto dto,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "User {UserId} creating environment '{Name}' for Project {ProjectId}",
                User.GetUserId(),
                dto.Name,
                dto.ProjectId);

            var userId = User.GetUserId();
            var environment = await _environmentService.CreateAsync(dto, userId, cancellationToken);

            var response = ApiResponse<EnvironmentDto>.SuccessResponse(
                environment,
                $"Environment '{dto.Name}' created successfully");

            response.CorrelationId = HttpContext.TraceIdentifier;

            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = environment.Id },
                response);
        }

        /// <summary>
        /// Update environment
        /// </summary>
        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<EnvironmentDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateEnvironmentDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;

            _logger.LogInformation(
                "User {UserId} updating environment {EnvironmentId}",
                User.GetUserId(),
                id);

            var userId = User.GetUserId();
            var environment = await _environmentService.UpdateAsync(dto, userId, cancellationToken);

            var response = ApiResponse<EnvironmentDto>.SuccessResponse(
                environment,
                "Environment updated successfully");

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        /// <summary>
        /// Delete environment
        /// </summary>
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "User {UserId} deleting environment {EnvironmentId}",
                User.GetUserId(),
                id);

            var userId = User.GetUserId();
            await _environmentService.DeleteAsync(id, userId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                "Environment deleted successfully");

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }
    }
}