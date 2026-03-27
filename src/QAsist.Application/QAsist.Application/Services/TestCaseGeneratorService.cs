//using Microsoft.Extensions.Logging;
//using QAsist.Application.Common.Exceptions;
//using QAsist.Application.DTOs;
//using QAsist.Application.Interfaces.IRepositories;
//using QAsist.Application.Interfaces.IServices;
//using System.Diagnostics;
//using System.Text.Json;

//namespace QAsist.Application.Services
//{
//    public class TestCaseGeneratorService : ITestCaseGeneratorService
//    {
//        private readonly IAiService _aiService;
//        private readonly IProjectRepository _projectRepository;
//        private readonly ITestCaseRepository _testCaseRepository;
//        private readonly ILogger<TestCaseGeneratorService> _logger;

//        private const string SystemPrompt = @"You are a Senior QA Engineer with 10+ years of experience in API testing.

//You deeply understand:
//- RESTful APIs
//- HTTP methods & status codes
//- Authentication & authorization
//- Validation, security, and edge cases

//Your task is to generate HIGH-QUALITY, PRACTICAL API test cases that can be directly used by QA engineers.";

//        private const string UserPromptTemplate = @"Analyze the following OpenAPI (Swagger) specification and generate comprehensive API test cases.

//REQUIREMENTS:
//1. Generate test cases for EACH endpoint and HTTP method.
//2. Include the following test types:
//   - Positive (valid requests)
//   - Negative (invalid inputs, missing fields)
//   - Authorization & authentication scenarios
//   - Boundary & edge cases
//   - Error handling (4xx, 5xx)
//3. Consider request body, headers, query parameters, and path parameters.
//4. Assume the API follows REST standards.
//5. Do NOT invent endpoints that are not present in the OpenAPI spec.
//6. Do NOT include explanations or comments.
//7. Return ONLY valid JSON.

//JSON FORMAT (STRICT):
//{
//  ""testCases"": [
//    {
//      ""endpoint"": ""/api/Auth/login"",
//      ""method"": ""POST"",
//      ""title"": ""Login with valid credentials"",
//      ""steps"": [
//        ""Send POST request with valid email and password in request body""
//      ],
//      ""expectedResult"": ""200 OK with access token and refresh token"",
//      ""priority"": ""High""
//    }
//  ]
//}

//OpenAPI Specification:
//<<<OPENAPI_JSON>>>";

//        public TestCaseGeneratorService(
//            IAiService aiService,
//            IProjectRepository projectRepository,
//            ITestCaseRepository testCaseRepository,
//            ILogger<TestCaseGeneratorService> logger)
//        {
//            _aiService = aiService;
//            _projectRepository = projectRepository;
//            _testCaseRepository = testCaseRepository;
//            _logger = logger;
//        }

//        public async Task<TestCaseGenerationResultDto> GenerateFromOpenApiAsync(
//            GenerateTestCasesRequestDto request,
//            Guid userId,
//            CancellationToken cancellationToken = default)
//        {
//            var stopwatch = Stopwatch.StartNew();

//            try
//            {
//                // 1. Validate project exists
//                var project = await ValidateProjectAsync(request.ProjectId, cancellationToken);

//                _logger.LogInformation(
//                    "Starting AI test case generation for Project: {ProjectId} ({ProjectName})",
//                    project.Id,
//                    project.Name);

//                // 2. Resolve OpenAPI spec source
//                var openApiJson = await ResolveOpenApiSpecAsync(request, cancellationToken);

//                // 3. Validate OpenAPI JSON
//                ValidateOpenApiJson(openApiJson);

//                // 4. Prepare AI prompt
//                var userPrompt = UserPromptTemplate.Replace("<<<OPENAPI_JSON>>>", openApiJson);

//                // 5. Call AI service
//                var aiResponse = await _aiService.GenerateJsonResponseAsync<AiTestCaseResponse>(
//                    SystemPrompt,
//                    userPrompt,
//                    cancellationToken);

//                stopwatch.Stop();

//                _logger.LogInformation(
//                    "Generated {Count} test cases for Project {ProjectName} in {Duration}ms",
//                    aiResponse.TestCases.Count,
//                    project.Name,
//                    stopwatch.ElapsedMilliseconds);

//                // 6. Optional: Save to database (Phase 2)
//                if (request.SaveToDatabase)
//                {
//                    await SaveTestCasesToDatabaseAsync(
//                        request.ProjectId,
//                        aiResponse.TestCases,
//                        userId,
//                        cancellationToken);
//                }

//                // 7. Return result
//                return new TestCaseGenerationResultDto
//                {
//                    ProjectId = project.Id,
//                    ProjectName = project.Name,
//                    TotalGenerated = aiResponse.TestCases.Count,
//                    TestCases = aiResponse.TestCases,
//                    Model = "gpt-4o",
//                    TokensUsed = 0, // Could be tracked from AI service
//                    ProcessingTime = stopwatch.Elapsed,
//                    GeneratedAt = DateTime.UtcNow
//                };
//            }
//            catch (NotFoundException)
//            {
//                throw;
//            }
//            catch (ValidationException)
//            {
//                throw;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating test cases for Project: {ProjectId}", request.ProjectId);
//                throw;
//            }
//        }

//        private async Task<Domain.Entities.Project> ValidateProjectAsync(
//            Guid projectId,
//            CancellationToken cancellationToken)
//        {
//            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

//            if (project == null)
//            {
//                throw new NotFoundException($"Project with ID {projectId} not found");
//            }

//            return project;
//        }

//        private async Task<string> ResolveOpenApiSpecAsync(
//            GenerateTestCasesRequestDto request,
//            CancellationToken cancellationToken)
//        {
//            // If OpenAPI JSON provided directly, use it
//            if (!string.IsNullOrWhiteSpace(request.OpenApiJson))
//            {
//                _logger.LogInformation("Using provided OpenAPI JSON");
//                return request.OpenApiJson;
//            }

//            // If UseDefaultSpec, we expect controller to have fetched it
//            if (request.UseDefaultSpec && !string.IsNullOrWhiteSpace(request.OpenApiJson))
//            {
//                _logger.LogInformation("Using default OpenAPI spec from controller");
//                return request.OpenApiJson;
//            }

//            throw new ValidationException("OpenAPI JSON must be provided");
//        }

//        private void ValidateOpenApiJson(string openApiJson)
//        {
//            if (string.IsNullOrWhiteSpace(openApiJson))
//            {
//                throw new ValidationException("OpenAPI JSON cannot be empty");
//            }

//            try
//            {
//                using var doc = JsonDocument.Parse(openApiJson);

//                if (!doc.RootElement.TryGetProperty("paths", out _) &&
//                    !doc.RootElement.TryGetProperty("openapi", out _) &&
//                    !doc.RootElement.TryGetProperty("swagger", out _))
//                {
//                    throw new ValidationException("Invalid OpenAPI specification format");
//                }
//            }
//            catch (JsonException ex)
//            {
//                throw new ValidationException($"Invalid JSON format: {ex.Message}");
//            }
//        }

//        private async Task SaveTestCasesToDatabaseAsync(
//            Guid projectId,
//            List<GeneratedTestCaseDto> testCases,
//            Guid userId,
//            CancellationToken cancellationToken)
//        {
//            var savedCount = await _testCaseRepository.BulkCreateAsync(
//                testCases,
//                projectId,
//                userId,
//                cancellationToken);

//            _logger.LogInformation(
//                "Saved {SavedCount} test cases to database for Project: {ProjectId}",
//                savedCount,
//                projectId);
//        }

//        private class AiTestCaseResponse
//        {
//            public List<GeneratedTestCaseDto> TestCases { get; set; } = new();
//        }
//    }
//}

using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using System.Diagnostics;
using System.Text.Json;

namespace QAsist.Application.Services
{
    public class TestCaseGeneratorService : ITestCaseGeneratorService
    {
        private readonly IAiService _aiService;
        private readonly IProjectRepository _projectRepository;
        private readonly ITestCaseRepository _testCaseRepository;
        private readonly ILogger<TestCaseGeneratorService> _logger;

        private const string SystemPrompt = @"You are a Senior QA Engineer with 10+ years of experience in API testing and test automation.

You deeply understand:
- RESTful APIs and HTTP protocols
- Test automation with Postman, REST Assured, and similar tools
- Request/response validation
- Authentication & authorization testing
- Edge cases, boundary conditions, and negative testing

Your task is to generate MACHINE-EXECUTABLE API test cases that can be run automatically to validate APIs.
These test cases will replace manual Postman collections and REST Assured test suites.";

        private const string UserPromptTemplate = @"Analyze the following OpenAPI (Swagger) specification and generate comprehensive, EXECUTABLE API test cases.

CRITICAL REQUIREMENTS:
1. Generate test cases for EACH endpoint and HTTP method in the spec
2. Each test case MUST be fully executable with:
   - Complete request specification (headers, body)
   - Precise validation criteria (status codes, response fields)
3. Include test types:
   - ✓ Positive scenarios (valid requests)
   - ✓ Negative scenarios (invalid inputs, missing fields, malformed data)
   - ✓ Authorization scenarios (missing token, invalid token, insufficient permissions)
   - ✓ Boundary & edge cases
   - ✓ Error handling (4xx, 5xx responses)
4. Do NOT invent endpoints not present in the OpenAPI spec
5. Return ONLY valid JSON with NO explanations or comments

STRICT JSON FORMAT:
{
  ""testCases"": [
    {
      ""endpoint"": ""/api/Auth/login"",
      ""method"": ""POST"",
      ""title"": ""Successful login with valid credentials"",
      ""description"": ""Verify user can login with correct email and password"",
      ""priority"": ""Critical"",
      ""requestHeaders"": {
        ""Content-Type"": ""application/json""
      },
      ""requestBody"": {
        ""email"": ""test@example.com"",
        ""password"": ""ValidPassword123!""
      },
      ""expectedResponse"": {
        ""statusCode"": 200,
        ""requiredFields"": [""data.accessToken"", ""data.refreshToken"", ""data.user.id"", ""data.user.email""],
        ""bodyContains"": [""accessToken"", ""refreshToken""],
        ""responseTimeMs"": 2000
      },
      ""requiresAuth"": false
    },
    {
      ""endpoint"": ""/api/Auth/login"",
      ""method"": ""POST"",
      ""title"": ""Login fails with invalid password"",
      ""description"": ""Verify system rejects invalid credentials"",
      ""priority"": ""High"",
      ""requestHeaders"": {
        ""Content-Type"": ""application/json""
      },
      ""requestBody"": {
        ""email"": ""test@example.com"",
        ""password"": ""WrongPassword""
      },
      ""expectedResponse"": {
        ""statusCode"": 401,
        ""bodyContains"": [""Invalid credentials""],
        ""forbiddenFields"": [""data.accessToken""]
      },
      ""requiresAuth"": false
    },
    {
      ""endpoint"": ""/api/Projects"",
      ""method"": ""GET"",
      ""title"": ""Get all projects successfully"",
      ""description"": ""Authorized user can retrieve project list"",
      ""priority"": ""High"",
      ""requestHeaders"": {},
      ""requestBody"": null,
      ""expectedResponse"": {
        ""statusCode"": 200,
        ""requiredFields"": [""success"", ""data""],
        ""responseTimeMs"": 1000
      },
      ""requiresAuth"": true
    },
    {
      ""endpoint"": ""/api/Projects"",
      ""method"": ""GET"",
      ""title"": ""Get projects fails without authentication"",
      ""description"": ""Verify endpoint requires authentication"",
      ""priority"": ""Critical"",
      ""requestHeaders"": {},
      ""requestBody"": null,
      ""expectedResponse"": {
        ""statusCode"": 401
      },
      ""requiresAuth"": false
    }
  ]
}

VALIDATION FIELD EXPLANATIONS:
- statusCode: Expected HTTP status code (200, 401, 404, etc.)
- requiredFields: JSON fields that MUST exist (use dot notation: ""data.user.email"")
- forbiddenFields: Fields that must NOT exist (e.g., sensitive data in error responses)
- bodyContains: Strings that must appear anywhere in response body
- bodyEquals: Exact JSON match (optional, use sparingly)
- responseTimeMs: Maximum acceptable response time in milliseconds

PRIORITY LEVELS:
- Critical: Core functionality, auth, data integrity
- High: Important features, error handling
- Medium: Secondary features, edge cases
- Low: Nice-to-have validations

OpenAPI Specification:
<<<OPENAPI_JSON>>>";

        public TestCaseGeneratorService(
            IAiService aiService,
            IProjectRepository projectRepository,
            ITestCaseRepository testCaseRepository,
            ILogger<TestCaseGeneratorService> logger)
        {
            _aiService = aiService;
            _projectRepository = projectRepository;
            _testCaseRepository = testCaseRepository;
            _logger = logger;
        }

        public async Task<TestCaseGenerationResultDto> GenerateFromOpenApiAsync(
            GenerateTestCasesRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Validate project exists
                var project = await ValidateProjectAsync(request.ProjectId, cancellationToken);

                _logger.LogInformation(
                    "Starting AI test case generation for Project: {ProjectId} ({ProjectName})",
                    project.Id,
                    project.Name);

                // 2. Resolve OpenAPI spec source
                var openApiJson = await ResolveOpenApiSpecAsync(request, cancellationToken);

                // 3. Validate OpenAPI JSON
                ValidateOpenApiJson(openApiJson);

                // 4. Prepare AI prompt
                var userPrompt = UserPromptTemplate.Replace("<<<OPENAPI_JSON>>>", openApiJson);

                // 5. Call AI service
                var aiResponse = await _aiService.GenerateJsonResponseAsync<AiTestCaseResponse>(
                    SystemPrompt,
                    userPrompt,
                    cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Generated {Count} executable test cases for Project {ProjectName} in {Duration}ms",
                    aiResponse.TestCases.Count,
                    project.Name,
                    stopwatch.ElapsedMilliseconds);

                // 6. Optional: Save to database (Phase 2)
                if (request.SaveToDatabase)
                {
                    // Convert ExecutableTestCaseDto to GeneratedTestCaseDto for backward compatibility
                    var legacyTestCases = aiResponse.TestCases.Select(tc => new GeneratedTestCaseDto
                    {
                        Endpoint = tc.Endpoint,
                        Method = tc.Method,
                        Title = tc.Title,
                        Steps = new List<string> { tc.Description ?? "Execute request" },
                        ExpectedResult = $"Status: {tc.ExpectedResponse.StatusCode}",
                        Priority = tc.Priority
                    }).ToList();

                    await SaveTestCasesToDatabaseAsync(
                        request.ProjectId,
                        legacyTestCases,
                        userId,
                        cancellationToken);
                }

                // 7. Return result with executable test cases
                return new TestCaseGenerationResultDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    TotalGenerated = aiResponse.TestCases.Count,
                    TestCases = aiResponse.TestCases.Select(tc => new GeneratedTestCaseDto
                    {
                        Endpoint = tc.Endpoint,
                        Method = tc.Method,
                        Title = tc.Title,
                        Steps = new List<string> { tc.Description ?? tc.Title },
                        ExpectedResult = $"Status {tc.ExpectedResponse.StatusCode}",
                        Priority = tc.Priority
                    }).ToList(),
                    ExecutableTestCases = aiResponse.TestCases, // NEW: Machine-executable format
                    Model = "gpt-4o",
                    TokensUsed = 0,
                    ProcessingTime = stopwatch.Elapsed,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test cases for Project: {ProjectId}", request.ProjectId);
                throw;
            }
        }

        private async Task<Domain.Entities.Project> ValidateProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

            if (project == null)
            {
                throw new NotFoundException($"Project with ID {projectId} not found");
            }

            return project;
        }

        private async Task<string> ResolveOpenApiSpecAsync(
            GenerateTestCasesRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.OpenApiJson))
            {
                _logger.LogInformation("Using provided OpenAPI JSON");
                return request.OpenApiJson;
            }

            if (request.UseDefaultSpec && !string.IsNullOrWhiteSpace(request.OpenApiJson))
            {
                _logger.LogInformation("Using default OpenAPI spec from controller");
                return request.OpenApiJson;
            }

            throw new ValidationException("OpenAPI JSON must be provided");
        }

        private void ValidateOpenApiJson(string openApiJson)
        {
            if (string.IsNullOrWhiteSpace(openApiJson))
            {
                throw new ValidationException("OpenAPI JSON cannot be empty");
            }

            try
            {
                using var doc = JsonDocument.Parse(openApiJson);

                if (!doc.RootElement.TryGetProperty("paths", out _) &&
                    !doc.RootElement.TryGetProperty("openapi", out _) &&
                    !doc.RootElement.TryGetProperty("swagger", out _))
                {
                    throw new ValidationException("Invalid OpenAPI specification format");
                }
            }
            catch (JsonException ex)
            {
                throw new ValidationException($"Invalid JSON format: {ex.Message}");
            }
        }

        private async Task SaveTestCasesToDatabaseAsync(
            Guid projectId,
            List<GeneratedTestCaseDto> testCases,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var savedCount = await _testCaseRepository.BulkCreateAsync(
                testCases,
                projectId,
                userId,
                cancellationToken);

            _logger.LogInformation(
                "Saved {SavedCount} test cases to database for Project: {ProjectId}",
                savedCount,
                projectId);
        }

        private class AiTestCaseResponse
        {
            public List<ExecutableTestCaseDto> TestCases { get; set; } = new();
        }
    }
}