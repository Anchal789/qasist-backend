using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace QAsist.Application.Services
{
    /// <summary>
    /// API Test Execution Engine - Replaces Postman/REST Assured
    /// </summary>
    public class TestExecutionService : ITestExecutionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IProjectRepository _projectRepository;
        private readonly IEnvironmentService _environmentService;
        private readonly ILogger<TestExecutionService> _logger;

        public TestExecutionService(
            IHttpClientFactory httpClientFactory,
            IProjectRepository projectRepository,
            IEnvironmentService environmentService,
            ILogger<TestExecutionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _projectRepository = projectRepository;
            _environmentService = environmentService;
            _logger = logger;
        }

        public async Task<TestExecutionSummaryDto> ExecuteTestCasesAsync(
            ExecuteTestCasesRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var batchStopwatch = Stopwatch.StartNew();
            var executionBatchId = Guid.NewGuid();

            _logger.LogInformation(
                "Starting test execution batch {BatchId} for Project {ProjectId} in {Environment}",
                executionBatchId,
                request.ProjectId,
                request.Environment);

            try
            {
                // 1. Validate request
                await ValidateExecutionRequestAsync(request, cancellationToken);

                // 2. Get project and environment
                var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
                    ?? throw new NotFoundException($"Project {request.ProjectId} not found");

                var environment = await _environmentService.GetByNameAsync(
                    request.ProjectId,
                    request.Environment,
                    cancellationToken)
                    ?? throw new NotFoundException($"Environment '{request.Environment}' not found for project");

                // 3. Security check - prevent execution on production
                if (environment.IsProduction && !environment.AllowExecution)
                {
                    throw new ValidationException(
                        "Execution on production environment is disabled. Enable it in environment settings.");
                }

                // 4. Get auth token (if needed)
                string? authToken = null; // TODO: Integrate with token service if needed

                // 5. Execute all test cases
                var results = new List<TestExecutionResultDto>();
                var startedAt = DateTime.UtcNow;

                foreach (var testCase in request.TestCases)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Test execution batch {BatchId} cancelled", executionBatchId);
                        break;
                    }

                    var result = await ExecuteSingleTestAsync(
                        testCase,
                        environment.BaseUrl,
                        authToken,
                        request.TimeoutSeconds,
                        cancellationToken);

                    results.Add(result);
                    // 🔥 Auto-capture token after successful login
                    if (result.Status == TestExecutionStatus.Passed &&
                        testCase.Endpoint.Equals("/api/Auth/login", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(result.ActualResponse ?? "");
                            var root = doc.RootElement;

                            if (root.TryGetProperty("data", out var dataElement) &&
                                dataElement.TryGetProperty("accessToken", out var tokenElement))
                            {
                                authToken = tokenElement.GetString();
                                _logger.LogInformation("Authentication token captured successfully.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to extract auth token from login response.");
                        }
                    }


                    // Fail fast if enabled
                    if (request.FailFast && result.Status == TestExecutionStatus.Failed)
                    {
                        _logger.LogWarning(
                            "Fail-fast enabled. Stopping execution after failure: {TestTitle}",
                            testCase.Title);
                        break;
                    }
                }

                batchStopwatch.Stop();
                var completedAt = DateTime.UtcNow;

                // 6. Build summary
                var summary = new TestExecutionSummaryDto
                {
                    ExecutionBatchId = executionBatchId,
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    Environment = request.Environment,
                    TotalTests = request.TestCases.Count,
                    PassedTests = results.Count(r => r.Status == TestExecutionStatus.Passed),
                    FailedTests = results.Count(r => r.Status == TestExecutionStatus.Failed),
                    SkippedTests = results.Count(r => r.Status == TestExecutionStatus.Skipped),
                    TotalExecutionTimeMs = (int)batchStopwatch.ElapsedMilliseconds,
                    Results = results,
                    StartedAt = startedAt,
                    CompletedAt = completedAt
                };

                summary.PassPercentage = summary.TotalTests > 0
                    ? Math.Round((decimal)summary.PassedTests / summary.TotalTests * 100, 2)
                    : 0;

                _logger.LogInformation(
                    "Test execution batch {BatchId} completed. Pass rate: {PassRate}% ({Passed}/{Total})",
                    executionBatchId,
                    summary.PassPercentage,
                    summary.PassedTests,
                    summary.TotalTests);

                // 7. TODO: Save results to database if requested
                // if (request.SaveResults) { await SaveExecutionResults(summary); }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing test batch {BatchId}", executionBatchId);
                throw;
            }
        }

        public async Task<TestExecutionResultDto> ExecuteSingleTestAsync(
            ExecutableTestCaseDto testCase,
            string baseUrl,
            string? authToken,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            var testExecutionId = Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();

            var result = new TestExecutionResultDto
            {
                TestExecutionId = testExecutionId,
                Title = testCase.Title,
                Endpoint = testCase.Endpoint,
                Method = testCase.Method,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug(
                    "Executing test: {Method} {Endpoint} - {Title}",
                    testCase.Method,
                    testCase.Endpoint,
                    testCase.Title);

                // 1. Build HTTP request
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));

                var requestUri = testCase.Endpoint.TrimStart('/');
                var requestMessage = new HttpRequestMessage(
                    new HttpMethod(testCase.Method.ToUpperInvariant()),
                    requestUri);

                // 2. Add headers
                foreach (var header in testCase.RequestHeaders)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // 3. Add authorization if required
                if (testCase.RequiresAuth && !string.IsNullOrEmpty(authToken))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }

                // 4. Add request body (if applicable)
                if (testCase.RequestBody != null &&
                    (testCase.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                     testCase.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                     testCase.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                {
                    var jsonBody = JsonSerializer.Serialize(testCase.RequestBody);
                    requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                // 5. Execute HTTP request
                HttpResponseMessage response;
                try
                {
                    response = await httpClient.SendAsync(requestMessage, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    stopwatch.Stop();
                    result.Status = TestExecutionStatus.Error;
                    result.FailureReason = $"Request timeout after {timeoutSeconds} seconds";
                    result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    stopwatch.Stop();
                    result.Status = TestExecutionStatus.Error;
                    result.FailureReason = $"HTTP request error: {ex.Message}";
                    result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    return result;
                }

                stopwatch.Stop();
                result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
                result.ActualStatusCode = (int)response.StatusCode;

                // 6. Read response body
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                result.ActualResponse = responseBody;

                // 7. Validate response
                var validationErrors = ValidateResponse(
                    response,
                    responseBody,
                    testCase.ExpectedResponse,
                    result.ExecutionTimeMs);

                if (validationErrors.Any())
                {
                    result.Status = TestExecutionStatus.Failed;
                    result.ValidationErrors = validationErrors;
                    result.FailureReason = string.Join("; ", validationErrors);
                }
                else
                {
                    result.Status = TestExecutionStatus.Passed;
                }

                _logger.LogDebug(
                    "Test execution completed: {Title} - {Status} ({ExecutionTime}ms)",
                    testCase.Title,
                    result.Status,
                    result.ExecutionTimeMs);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error executing test: {Title}", testCase.Title);

                result.Status = TestExecutionStatus.Error;
                result.FailureReason = $"Unexpected error: {ex.Message}";
                result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        public async Task ValidateExecutionRequestAsync(
            ExecuteTestCasesRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (request.ProjectId == Guid.Empty)
            {
                errors.Add("ProjectId is required");
            }

            if (string.IsNullOrWhiteSpace(request.Environment))
            {
                errors.Add("Environment is required");
            }

            if (request.TestCases == null || !request.TestCases.Any())
            {
                errors.Add("At least one test case is required");
            }

            if (request.TimeoutSeconds < 1 || request.TimeoutSeconds > 300)
            {
                errors.Add("Timeout must be between 1 and 300 seconds");
            }

            if (errors.Any())
            {
                throw new ValidationException(string.Join("; ", errors));
            }

            await Task.CompletedTask;
        }

        public Task<IEnumerable<TestExecutionSummaryDto>> GetExecutionHistoryAsync(
            Guid projectId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // TODO: Implement database retrieval
            throw new NotImplementedException("Execution history will be implemented in Phase 2");
        }

        public Task<TestExecutionSummaryDto?> GetExecutionDetailsAsync(
            Guid executionBatchId,
            CancellationToken cancellationToken = default)
        {
            // TODO: Implement database retrieval
            throw new NotImplementedException("Execution details will be implemented in Phase 2");
        }

        /// <summary>
        /// Validate API response against expected criteria
        /// </summary>
        private List<string> ValidateResponse(
            HttpResponseMessage response,
            string responseBody,
            ExpectedResponseDto expected,
            int actualExecutionTimeMs)
        {
            var errors = new List<string>();

            // 1. Validate status code
            if ((int)response.StatusCode != expected.StatusCode)
            {
                errors.Add($"Expected status {expected.StatusCode}, got {(int)response.StatusCode}");
            }

            // 2. Validate response time (if specified)
            if (expected.ResponseTimeMs.HasValue && actualExecutionTimeMs > expected.ResponseTimeMs.Value)
            {
                errors.Add(
                    $"Response time {actualExecutionTimeMs}ms exceeds limit of {expected.ResponseTimeMs}ms");
            }

            // 3. Validate body contains (substring checks)
            foreach (var expectedText in expected.BodyContains)
            {
                if (!responseBody.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Response body does not contain expected text: '{expectedText}'");
                }
            }

            // 4. Validate JSON structure (if response is JSON)
            if (IsJsonResponse(response))
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    // Check required fields
                    foreach (var field in expected.RequiredFields)
                    {
                        if (!HasJsonProperty(root, field))
                        {
                            errors.Add($"Required field missing: '{field}'");
                        }
                    }

                    // Check forbidden fields
                    foreach (var field in expected.ForbiddenFields)
                    {
                        if (HasJsonProperty(root, field))
                        {
                            errors.Add($"Forbidden field present: '{field}'");
                        }
                    }

                    // 5. Validate exact body equality (if specified)
                    if (expected.BodyEquals != null)
                    {
                        var expectedJson = JsonSerializer.Serialize(expected.BodyEquals);
                        var actualJson = JsonSerializer.Serialize(root);

                        if (!JsonEquals(expectedJson, actualJson))
                        {
                            errors.Add("Response body does not match expected body");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    errors.Add($"Invalid JSON response: {ex.Message}");
                }
            }

            return errors;
        }

        private bool IsJsonResponse(HttpResponseMessage response)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            return contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private bool HasJsonProperty(JsonElement element, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            var current = element;

            foreach (var part in parts)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var child))
                {
                    current = child;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool JsonEquals(string json1, string json2)
        {
            try
            {
                using var doc1 = JsonDocument.Parse(json1);
                using var doc2 = JsonDocument.Parse(json2);

                // Serialize both and compare strings for compatibility
                var normalized1 = JsonSerializer.Serialize(doc1.RootElement);
                var normalized2 = JsonSerializer.Serialize(doc2.RootElement);

                return normalized1 == normalized2;
            }
            catch
            {
                return false;
            }
        }
    }
}