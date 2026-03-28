using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs.Monitoring;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Enums;

namespace QAsist.Api.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;
        private readonly IUptimeCalculator _uptimeCalculator;
        private readonly IRedisService _redis;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(
            IMonitoringService monitoringService,
            IUptimeCalculator uptimeCalculator,
            IRedisService redis,
            ILogger<MonitoringController> logger)
        {
            _monitoringService = monitoringService;
            _uptimeCalculator = uptimeCalculator;
            _redis = redis;
            _logger = logger;
        }

        [HttpGet("endpoints/project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MonitoredEndpointDto>>>> GetByProjectAsync(
            Guid projectId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Monitoring] GetByProject started. ProjectId={Id}", projectId);
            try
            {
                var endpoints = await _monitoringService
                    .GetEndpointsByProjectAsync(projectId, cancellationToken);
                var response = ApiResponse<IEnumerable<MonitoredEndpointDto>>
                    .SuccessResponse(endpoints);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Monitoring] GetByProject succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Monitoring] GetByProject failed. ProjectId={Id}", projectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] GetByProject completed.");
            }
        }

        [HttpGet("endpoints/{id:guid}")]
        public async Task<ActionResult<ApiResponse<MonitoredEndpointDto>>> GetByIdAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Monitoring] GetById started. Id={Id}", id);
            try
            {
                var endpoint = await _monitoringService
                    .GetEndpointByIdAsync(id, cancellationToken);
                var response = ApiResponse<MonitoredEndpointDto>.SuccessResponse(endpoint);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Monitoring] GetById succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] GetById failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] GetById completed. Id={Id}", id);
            }
        }

        [HttpPost("endpoints")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<MonitoredEndpointDto>>> CreateAsync(
            [FromBody] CreateMonitoredEndpointDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Monitoring] Create started. Name={Name} Url={Url} User={UserId}",
                dto.Name, dto.Url, userId);
            try
            {
                var endpoint = await _monitoringService
                    .CreateEndpointAsync(dto, userId, cancellationToken);
                var response = ApiResponse<MonitoredEndpointDto>.SuccessResponse(
                    endpoint, "Endpoint registered for monitoring.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Monitoring] Create succeeded. EndpointId={Id}", endpoint.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Monitoring] Create failed. Name={Name}", dto.Name);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] Create completed.");
            }
        }

        [HttpPut("endpoints/{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<MonitoredEndpointDto>>> UpdateAsync(
            Guid id,
            [FromBody] UpdateMonitoredEndpointDto dto,
            CancellationToken cancellationToken)
        {
            dto.Id = id;
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Monitoring] Update started. Id={Id} User={UserId}", id, userId);
            try
            {
                var endpoint = await _monitoringService
                    .UpdateEndpointAsync(dto, userId, cancellationToken);
                var response = ApiResponse<MonitoredEndpointDto>.SuccessResponse(
                    endpoint, "Monitoring endpoint updated.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Monitoring] Update succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] Update failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] Update completed. Id={Id}", id);
            }
        }

        [HttpDelete("endpoints/{id:guid}")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
            Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[Monitoring] Delete started. Id={Id} User={UserId}", id, userId);
            try
            {
                await _monitoringService.DeleteEndpointAsync(id, userId, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, "Monitoring endpoint deleted.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Monitoring] Delete succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] Delete failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] Delete completed. Id={Id}", id);
            }
        }

        [HttpGet("endpoints/{id:guid}/stats")]
        public async Task<ActionResult<ApiResponse<EndpointStatsDto>>> GetStatsAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Monitoring] GetStats started. Id={Id}", id);
            try
            {
                var stats = await _uptimeCalculator.GetStatsAsync(id, cancellationToken);
                var response = ApiResponse<EndpointStatsDto>.SuccessResponse(stats);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Monitoring] GetStats succeeded. Uptime={Uptime}%",
                    stats.UptimePercent24h);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] GetStats failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] GetStats completed. Id={Id}", id);
            }
        }

        [HttpGet("endpoints/{id:guid}/logs")]
        public async Task<ActionResult<PagedApiResponse<IEnumerable<MonitoringLogDto>>>> GetLogsAsync(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[Monitoring] GetLogs started. Id={Id} Page={Page}", id, pageNumber);
            try
            {
                var (logs, total) = await _monitoringService
                    .GetLogsPagedAsync(id, pageNumber, pageSize, cancellationToken);
                var response = PagedApiResponse<IEnumerable<MonitoringLogDto>>
                    .SuccessResponse(logs, total, pageNumber, pageSize);
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[Monitoring] GetLogs succeeded. Total={Total}", total);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] GetLogs failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] GetLogs completed. Id={Id}", id);
            }
        }

        [HttpPost("endpoints/{id:guid}/check")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin)]
        public async Task<ActionResult<ApiResponse<object>>> TriggerCheckAsync(
            Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Monitoring] TriggerCheck started. Id={Id}", id);
            try
            {
                await _monitoringService.CheckEndpointAsync(id, cancellationToken);
                var response = ApiResponse<object>.SuccessResponse(
                    null, "Manual check triggered.");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[Monitoring] TriggerCheck succeeded. Id={Id}", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Monitoring] TriggerCheck failed. Id={Id}", id);
                throw;
            }
            finally
            {
                _logger.LogDebug("[Monitoring] TriggerCheck completed. Id={Id}", id);
            }
        }
    }
}