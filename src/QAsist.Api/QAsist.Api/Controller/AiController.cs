using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QAsist.Api.Extensions;
using QAsist.Api.Filters;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly ITestCaseGeneratorService _testCaseGenerator;
    private readonly ITestCaseExportService _exportService;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<AiController> _logger;

    public AiController(
        ITestCaseGeneratorService testCaseGenerator,
        ITestCaseExportService exportService,
        IProjectRepository projectRepository,
        ILogger<AiController> logger)
    {
        _testCaseGenerator = testCaseGenerator;
        _exportService = exportService;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generate API test cases from OpenAPI (Swagger) specification using AI
    /// </summary>
    [HttpPost("generate-test-cases")]
    [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.QALead)]
    public async Task<ActionResult<ApiResponse<TestCaseGenerationResultDto>>> GenerateTestCasesAsync(
        [FromBody] GenerateTestCasesRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} generating test cases for Project {ProjectId}",
            User.GetUserId(),
            request.ProjectId);

        // If UseDefaultSpec is true and OpenApiJson is empty, fetch it
        if (request.UseDefaultSpec && string.IsNullOrWhiteSpace(request.OpenApiJson))
        {
            request.OpenApiJson = await FetchDefaultOpenApiSpecAsync();
        }

        var userId = User.GetUserId();
        var result = await _testCaseGenerator.GenerateFromOpenApiAsync(request, userId, cancellationToken);

        var response = ApiResponse<TestCaseGenerationResultDto>.SuccessResponse(
            result,
            $"Successfully generated {result.TotalGenerated} test cases for {result.ProjectName}");

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }

    /// <summary>
    /// Get current OpenAPI specification from running API
    /// </summary>
    [HttpGet("openapi-spec")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetOpenApiSpecAsync()
    {
        try
        {
            var openApiJson = await FetchDefaultOpenApiSpecAsync();

            var response = ApiResponse<object>.SuccessResponse(
                new { openApiJson = openApiJson, url = $"{Request.Scheme}://{Request.Host}/swagger/v1/swagger.json" },
                "OpenAPI specification retrieved successfully");

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OpenAPI specification");

            return Ok(ApiResponse<object>.ErrorResponse(
                "Failed to retrieve OpenAPI specification",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Export generated test cases to Excel (.xlsx)
    /// </summary>
    [HttpPost("export-test-cases/excel")]
    [AuthorizeRoles(UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.QALead)]
    public async Task<IActionResult> ExportTestCasesToExcelAsync(
        [FromBody] ExportTestCasesRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} exporting {Count} test cases for Project {ProjectId}",
            User.GetUserId(),
            request.TestCases.Count,
            request.ProjectId);

        // Validate project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project == null)
        {
            var errorResponse = ApiResponse<object>.ErrorResponse(
                $"Project with ID {request.ProjectId} not found");
            errorResponse.CorrelationId = HttpContext.TraceIdentifier;

            return NotFound(errorResponse);
        }

        // Generate Excel file
        var (fileBytes, fileName) = await _exportService.ExportToExcelAsync(
            request.ProjectId,
            project.Name,
            request.TestCases,
            cancellationToken);

        _logger.LogInformation(
            "Excel export completed for Project {ProjectName}. File: {FileName}",
            project.Name,
            fileName);

        // Return file as downloadable content
        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task<string> FetchDefaultOpenApiSpecAsync()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var swaggerUrl = $"{baseUrl}/swagger/v1/swagger.json";

        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync(swaggerUrl);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Swagger JSON is empty");
        }

        return json;
    }
}