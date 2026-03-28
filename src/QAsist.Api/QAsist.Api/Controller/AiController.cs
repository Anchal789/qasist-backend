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

namespace QAsist.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class AiController : ControllerBase
    {
        private readonly ITestCaseGeneratorService _testCaseGenerator;
        private readonly ITestCaseExportService _exportService;
        private readonly IProjectRepository _projectRepository;
        private readonly IHttpClientFactory _httpClientFactory;   // ← FIXED: use factory not new HttpClient()
        private readonly ILogger<AiController> _logger;

        public AiController(
            ITestCaseGeneratorService testCaseGenerator,
            ITestCaseExportService exportService,
            IProjectRepository projectRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<AiController> logger)
        {
            _testCaseGenerator = testCaseGenerator;
            _exportService = exportService;
            _projectRepository = projectRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("generate-test-cases")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QALead)]
        public async Task<ActionResult<ApiResponse<TestCaseGenerationResultDto>>> GenerateTestCasesAsync(
            [FromBody] GenerateTestCasesRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[AI] GenerateTestCases started. ProjectId={Id} User={UserId}",
                request.ProjectId, userId);
            try
            {
                if (request.UseDefaultSpec && string.IsNullOrWhiteSpace(request.OpenApiJson))
                    request.OpenApiJson = await FetchDefaultOpenApiSpecAsync();

                var result = await _testCaseGenerator
                    .GenerateFromOpenApiAsync(request, userId, cancellationToken);

                var response = ApiResponse<TestCaseGenerationResultDto>.SuccessResponse(
                    result,
                    $"Generated {result.TotalGenerated} test cases for {result.ProjectName}");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation(
                    "[AI] GenerateTestCases succeeded. Count={Count}", result.TotalGenerated);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AI] GenerateTestCases failed. ProjectId={Id}", request.ProjectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[AI] GenerateTestCases completed.");
            }
        }

        [HttpGet("openapi-spec")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> GetOpenApiSpecAsync()
        {
            _logger.LogInformation("[AI] GetOpenApiSpec started.");
            try
            {
                var openApiJson = await FetchDefaultOpenApiSpecAsync();
                var response = ApiResponse<object>.SuccessResponse(
                    new
                    {
                        openApiJson,
                        url = $"{Request.Scheme}://{Request.Host}/swagger/v1/swagger.json"
                    },
                    "OpenAPI specification retrieved successfully");
                response.CorrelationId = HttpContext.TraceIdentifier;

                _logger.LogInformation("[AI] GetOpenApiSpec succeeded.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI] GetOpenApiSpec failed.");
                return Ok(ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve OpenAPI specification",
                    new List<string> { ex.Message }));
            }
            finally
            {
                _logger.LogDebug("[AI] GetOpenApiSpec completed.");
            }
        }

        [HttpPost("export-test-cases/excel")]
        [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin,
                        UserRole.ProjectManager, UserRole.QALead)]
        public async Task<IActionResult> ExportTestCasesToExcelAsync(
            [FromBody] ExportTestCasesRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            _logger.LogInformation(
                "[AI] ExportToExcel started. ProjectId={Id} Count={Count} User={UserId}",
                request.ProjectId, request.TestCases.Count, userId);
            try
            {
                var project = await _projectRepository
                    .GetByIdAsync(request.ProjectId, cancellationToken);

                if (project is null)
                {
                    _logger.LogWarning(
                        "[AI] Project not found. ProjectId={Id}", request.ProjectId);
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        $"Project {request.ProjectId} not found"));
                }

                var (fileBytes, fileName) = await _exportService.ExportToExcelAsync(
                    request.ProjectId, project.Name, request.TestCases, cancellationToken);

                _logger.LogInformation(
                    "[AI] ExportToExcel succeeded. File={FileName}", fileName);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AI] ExportToExcel failed. ProjectId={Id}", request.ProjectId);
                throw;
            }
            finally
            {
                _logger.LogDebug("[AI] ExportToExcel completed.");
            }
        }

        // ── FIX: use IHttpClientFactory instead of new HttpClient() ──────────
        private async Task<string> FetchDefaultOpenApiSpecAsync()
        {
            var swaggerUrl = $"{Request.Scheme}://{Request.Host}/swagger/v1/swagger.json";
            var client = _httpClientFactory.CreateClient("OpenApiFetcher");
            var json = await client.GetStringAsync(swaggerUrl);

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("Swagger JSON is empty");

            return json;
        }
    }
}