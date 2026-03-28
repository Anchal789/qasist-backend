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

namespace QAsist.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
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

        // ── GET /api/v1/environments/project/{projectId} ──────────────────────
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<EnvironmentDto>>>> GetByProjectAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Environments] GetByProject started. ProjectId={Id}", projectId);
            try
            {
                var environments = await _environmentService
                    .GetByProjectAsync(projectId, cancellationToken);
                var response = ApiResponse<IEnumerable<EnvironmentDto>>
                    .SuccessResponse(environments);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Environments] GetByProject succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Environments] GetByProject failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug(
                    "[Environments] GetByProject completed. ProjectId={Id}", projectId);
            }
        }

        // ── GET /api/v1/environments/{id} ─────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<EnvironmentDto>>> GetByIdAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Environments] GetById started. Id={Id}", id);
            try
            {
                var environment = await _environmentService.GetByIdAsync(id, cancellationToken);
                var response = ApiResponse<EnvironmentDto>.SuccessResponse(environment);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Environments] GetById succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Environments] GetById failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Environments] GetById completed. Id={Id}", id);
            }
        }

        // ── POST /api/v1/environments ─────────────────────────────────────────
        [HttpPost]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<EnvironmentDto>>> CreateAsync(
            [FromBody] CreateEnvironmentDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Environments] Create started. Name={Name} ProjectId={ProjectId} User={UserId}",
                dto.Name, dto.ProjectId, userId);
            try
            {
                var environment = await _environmentService
                    .CreateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<EnvironmentDto>.SuccessResponse(
                    environment, $"Environment '{dto.Name}' created successfully.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Environments] Create succeeded. EnvironmentId={Id}", environment.Id);

                // Use Ok() instead of CreatedAtAction to avoid route resolution issues
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Environments] Create failed. Name={Name}", dto.Name);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Environments] Create completed.");
            }
        }

        // ── PUT /api/v1/environments/{id} ─────────────────────────────────────
        [HttpPut("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<EnvironmentDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateEnvironmentDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Environments] Update started. Id={Id} User={UserId}", id, userId);
            try
            {
                var environment = await _environmentService
                    .UpdateAsync(dto, userId, cancellationToken);
                var response = ApiResponse<EnvironmentDto>.SuccessResponse(
                    environment, "Environment updated successfully.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Environments] Update succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Environments] Update failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Environments] Update completed. Id={Id}", id);
            }
        }

        // ── DELETE /api/v1/environments/{id} ──────────────────────────────────
        [HttpDelete("{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Environments] Delete started. Id={Id} User={UserId}", id, userId);
            try
            {
                await _environmentService.DeleteAsync(id, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, "Environment deleted successfully.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Environments] Delete succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Environments] Delete failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Environments] Delete completed. Id={Id}", id);
            }
        }
    }
}