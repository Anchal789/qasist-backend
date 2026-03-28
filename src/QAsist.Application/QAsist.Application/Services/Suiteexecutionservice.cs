using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static QAsist.Application.DTOs.Enginedtos;
using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Application.Services
{
    public class SuiteExecutionService : ISuiteExecutionService
    {
        private readonly ITestSuiteRepository _suiteRepository;
        private readonly ITestSuiteMappingRepository _mappingRepository;
        private readonly ITestCaseRepository _testCaseRepository;
        private readonly IEnvironmentRepository _environmentRepository;
        private readonly IExecutionRepository _executionRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SuiteExecutionService> _logger;

        // HTTP client name — registered in Infrastructure DI with Polly
        private const string HttpClientName = "StepExecutor";

        // Execution status constants
        private const string StatusPass = "Pass";
        private const string StatusFail = "Fail";
        private const string StatusError = "Error";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public SuiteExecutionService(
            ITestSuiteRepository suiteRepository,
            ITestSuiteMappingRepository mappingRepository,
            ITestCaseRepository testCaseRepository,
            IEnvironmentRepository environmentRepository,
            IExecutionRepository executionRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<SuiteExecutionService> logger)
        {
            _suiteRepository = suiteRepository;
            _mappingRepository = mappingRepository;
            _testCaseRepository = testCaseRepository;
            _environmentRepository = environmentRepository;
            _executionRepository = executionRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ── EXECUTE ───────────────────────────────────────────────────────────
        public async Task<SuiteExecutionBatchDto> ExecuteAsync(
            Guid suiteId,
            ExecuteSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "=== Starting suite execution: SuiteId={SuiteId} Env={EnvId} " +
                    "FailFast={FF} Parallel={P} ===",
                    suiteId, dto.EnvironmentId, dto.FailFast, dto.ParallelCases);

                // 1. Validate suite exists
                var suite = await _suiteRepository.GetByIdAsync(suiteId, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                // 2. Fetch environment (BaseUrl + GlobalHeaders)
                var environment = await _environmentRepository
                    .GetByIdAsync(dto.EnvironmentId, cancellationToken);
                if (environment is null)
                    throw new NotFoundException(ResponseMessages.EnvironmentNotFound);

                _logger.LogInformation(
                    "Environment loaded: {Name} BaseUrl={BaseUrl}",
                    environment.Name, environment.BaseUrl);

                // 3. Get all mapped test cases (ordered)
                var mappings = (await _mappingRepository
                    .GetBySuiteWithDetailsAsync(suiteId, cancellationToken))
                    .Where(m => m.IsEnabled)
                    .OrderBy(m => m.Order)
                    .ToList();

                if (!mappings.Any())
                {
                    _logger.LogWarning(
                        "Suite {SuiteId} has no enabled test cases. Nothing to execute.",
                        suiteId);
                }

                // 4. Create execution batch record
                var batchId = Guid.NewGuid();
                var batch = BuildBatch(batchId, suiteId, suite.ProjectId,
                    dto.EnvironmentId, dto.FailFast, dto.ParallelCases, userId);

                await _executionRepository.CreateBatchAsync(batch, cancellationToken);

                _logger.LogInformation(
                    "Execution batch {BatchId} created. Running {Count} test cases.",
                    batchId, mappings.Count);

                // 5. Execute (fire-and-forget background task)
                _ = Task.Run(async () =>
                {
                    await RunExecutionAsync(
                        batch, mappings, environment, dto, userId);
                }, cancellationToken);

                // 6. Return immediately — client polls for results
                return new SuiteExecutionBatchDto
                {
                    BatchId = batchId,
                    SuiteId = suiteId,
                    ProjectId = suite.ProjectId,
                    Status = "Running",
                    SuiteType = "mapped",
                    TotalSteps = mappings.Count,
                    StartedAt = batch.StartedAt
                };
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to start execution for suite {SuiteId}", suiteId);
                throw;
            }
        }

        // ── GET BATCH ─────────────────────────────────────────────────────────
        public async Task<SuiteExecutionBatchDto> GetBatchAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var batch = await _executionRepository
                    .GetBatchByIdAsync(batchId, cancellationToken);
                if (batch is null)
                    throw new NotFoundException(ResponseMessages.ExecutionNotFound);

                return new SuiteExecutionBatchDto
                {
                    BatchId = batch.Id,
                    SuiteId = batch.TestSuiteId,
                    ProjectId = batch.ProjectId,
                    Status = batch.Status.ToString(),
                    TotalSteps = batch.TotalSteps,
                    PassedSteps = batch.PassedSteps,
                    FailedSteps = batch.FailedSteps,
                    SkippedSteps = batch.SkippedSteps,
                    ErrorSteps = batch.ErrorSteps,
                    PassPercentage = batch.PassPercentage,
                    TotalDurationMs = batch.TotalDurationMs,
                    StartedAt = batch.StartedAt,
                    CompletedAt = batch.CompletedAt
                };
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting batch {BatchId}", batchId);
                throw;
            }
        }

        // ── GET RESULTS ───────────────────────────────────────────────────────
        public async Task<(IEnumerable<ExecutionStepResultDto> Results, int Total)> GetResultsAsync(
            Guid batchId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching results for batch {BatchId} page={Page}",
                    batchId, pageNumber);

                var (results, total) = await _executionRepository
                    .GetResultsByBatchPagedAsync(batchId, pageNumber, pageSize, cancellationToken);

                var dtos = results.Select(r => new ExecutionStepResultDto
                {
                    Id = r.Id,
                    BatchId = r.BatchId,
                    TestCaseId = r.TestCaseId,
                    Status = r.Status.ToString(),
                    DurationMs = r.DurationMs,
                    ErrorMessage = r.ErrorMessage,
                    CompletedAt = r.ExecutedAt
                });

                return (dtos, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching results for batch {BatchId}", batchId);
                throw;
            }
        }

        // ── GET HISTORY ───────────────────────────────────────────────────────
        public async Task<(IEnumerable<SuiteExecutionBatchDto> Batches, int Total)> GetHistoryAsync(
            Guid suiteId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (batches, total) = await _executionRepository
                    .GetBatchHistoryAsync(suiteId, pageNumber, pageSize, CancellationToken.None);

                var dtos = batches.Select(b => new SuiteExecutionBatchDto
                {
                    BatchId = b.Id,
                    SuiteId = b.TestSuiteId,
                    ProjectId = b.ProjectId,
                    Status = b.Status.ToString(),
                    TotalSteps = b.TotalSteps,
                    PassedSteps = b.PassedSteps,
                    FailedSteps = b.FailedSteps,
                    SkippedSteps = b.SkippedSteps,
                    ErrorSteps = b.ErrorSteps,
                    PassPercentage = b.PassPercentage,
                    TotalDurationMs = b.TotalDurationMs,
                    StartedAt = b.StartedAt,
                    CompletedAt = b.CompletedAt
                });

                return (dtos, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching history for suite {SuiteId}", suiteId);
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE: Core execution logic
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Runs all test cases either sequentially or in parallel.
        /// Always runs in background — updates batch on completion.
        /// </summary>
        private async Task RunExecutionAsync(
            Domain.Entities.ExecutionBatch batch,
            List<SuiteTestCaseMappingDto> mappings,
            Domain.Entities.Environment environment,
            ExecuteSuiteDto dto,
            Guid userId)
        {
            var suiteTimer = Stopwatch.StartNew();
            var results = new List<ExecutionStepResultDto>();

            try
            {
                _logger.LogInformation(
                    "Execution started. BatchId={BatchId} Mode={Mode}",
                    batch.Id, dto.ParallelCases ? "Parallel" : "Sequential");

                if (dto.ParallelCases)
                {
                    // ── PARALLEL ──────────────────────────────────────────────
                    _logger.LogInformation(
                        "Running {Count} test cases in PARALLEL", mappings.Count);

                    var tasks = mappings.Select(m =>
                        ExecuteTestCaseAsync(m, environment, batch.Id, userId));

                    var parallelResults = await Task.WhenAll(tasks);
                    results.AddRange(parallelResults);
                }
                else
                {
                    // ── SEQUENTIAL ────────────────────────────────────────────
                    _logger.LogInformation(
                        "Running {Count} test cases SEQUENTIALLY", mappings.Count);

                    foreach (var mapping in mappings)
                    {
                        var result = await ExecuteTestCaseAsync(
                            mapping, environment, batch.Id, userId);

                        results.Add(result);

                        // FailFast: abort if this step failed
                        if (dto.FailFast && result.Status == StatusFail)
                        {
                            _logger.LogWarning(
                                "FailFast triggered. TestCase {TestCaseId} failed. " +
                                "Aborting remaining {Count} test cases.",
                                mapping.TestCaseId,
                                mappings.Count - mappings.IndexOf(mapping) - 1);
                            break;
                        }
                    }
                }

                suiteTimer.Stop();

                // Update batch with final counts
                batch.Status = results.Any(r => r.Status == StatusFail)
                    ? Domain.Enums.ExecutionStatus.Failed
                    : Domain.Enums.ExecutionStatus.Completed;
                batch.TotalSteps = results.Count;
                batch.PassedSteps = results.Count(r => r.Status == StatusPass);
                batch.FailedSteps = results.Count(r => r.Status == StatusFail);
                batch.ErrorSteps = results.Count(r => r.Status == StatusError);
                batch.CompletedAt = DateTime.UtcNow;

                await _executionRepository.UpdateBatchAsync(batch, CancellationToken.None);

                _logger.LogInformation(
                    "=== Execution complete. BatchId={BatchId} " +
                    "Pass={P} Fail={F} Error={E} Pass%={Pct}% Duration={Ms}ms ===",
                    batch.Id,
                    batch.PassedSteps, batch.FailedSteps, batch.ErrorSteps,
                    batch.PassPercentage,
                    suiteTimer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                suiteTimer.Stop();
                _logger.LogError(ex,
                    "Execution failed for batch {BatchId}", batch.Id);

                batch.Status = Domain.Enums.ExecutionStatus.Failed;
                batch.CompletedAt = DateTime.UtcNow;
                await _executionRepository.UpdateBatchAsync(batch, CancellationToken.None);
            }
        }

        /// <summary>
        /// Executes one TestCase and stores the StepResult.
        ///
        /// Requirement: fullUrl = BaseUrl + endpoint
        /// Requirement: Validates StatusCode + ResponseBody + ResponseTime
        /// Requirement: Returns Pass / Fail / Error + failure reason
        /// </summary>
        private async Task<ExecutionStepResultDto> ExecuteTestCaseAsync(
            SuiteTestCaseMappingDto mapping,
            Domain.Entities.Environment environment,
            Guid batchId,
            Guid userId)
        {
            var stepTimer = Stopwatch.StartNew();
            var startedAt = DateTime.UtcNow;
            var resultId = Guid.NewGuid();
            string status = StatusError;
            string? errorMessage = null;
            string? failureReason = null;
            int? statusCode = null;
            string? responseBody = null;

            // Build full URL: BaseUrl + endpoint
            var baseUrl = environment.BaseUrl.TrimEnd('/');
            var endpoint = mapping.Endpoint.TrimStart('/');
            var fullUrl = $"{baseUrl}/{endpoint}";

            _logger.LogInformation(
                "  → Executing [{Order}] {Method} {FullUrl} (TestCase: {Title})",
                mapping.Order, mapping.Method, fullUrl, mapping.TestCaseTitle);

            try
            {
                // Load full test case for headers, body, assertions
                var testCase = await _testCaseRepository
                    .GetByIdAsync(mapping.TestCaseId, CancellationToken.None);

                if (testCase is null)
                {
                    _logger.LogError(
                        "TestCase {TestCaseId} not found in DB!", mapping.TestCaseId);
                    return BuildStepResult(resultId, batchId, mapping, fullUrl,
                        StatusError, null, null, startedAt, DateTime.UtcNow, 0,
                        "TestCase not found in database.", null);
                }

                // Build HttpRequestMessage using IHttpClientFactory
                var client = _httpClientFactory.CreateClient(HttpClientName);
                var request = BuildRequest(testCase, fullUrl, environment);

                // Send request with timeout handling
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromMilliseconds(testCase.ExpectedResponseTimeMs ?? 10_000));

                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    stepTimer.Stop();
                    _logger.LogWarning(
                        "  ✗ TIMEOUT [{Order}] {Method} {Url} after {Ms}ms",
                        mapping.Order, mapping.Method, fullUrl, stepTimer.ElapsedMilliseconds);

                    return BuildStepResult(resultId, batchId, mapping, fullUrl,
                        StatusError, null, null, startedAt, DateTime.UtcNow,
                        (int)stepTimer.ElapsedMilliseconds,
                        $"Request timed out after {testCase.ExpectedResponseTimeMs ?? 10_000}ms.",
                        null);
                }
                catch (HttpRequestException ex)
                {
                    stepTimer.Stop();
                    _logger.LogWarning(
                        "  ✗ NETWORK ERROR [{Order}] {Method} {Url}: {Msg}",
                        mapping.Order, mapping.Method, fullUrl, ex.Message);

                    return BuildStepResult(resultId, batchId, mapping, fullUrl,
                        StatusError, null, null, startedAt, DateTime.UtcNow,
                        (int)stepTimer.ElapsedMilliseconds,
                        $"Network error: {ex.Message}", null);
                }

                stepTimer.Stop();
                statusCode = (int)response.StatusCode;
                responseBody = await response.Content.ReadAsStringAsync();

                // Truncate response body at 100KB
                if (responseBody.Length > 102_400)
                    responseBody = responseBody[..102_400] + "\n[TRUNCATED]";

                // ── VALIDATION LOGIC ──────────────────────────────────────────
                // Requirement: Validate StatusCode + ResponseBody + ResponseTime
                var failures = new List<string>();

                // 1. Validate Status Code
                if (testCase.ExpectedStatusCode.HasValue)
                {
                    if (statusCode != testCase.ExpectedStatusCode.Value)
                    {
                        failures.Add(
                            $"Status code: expected {testCase.ExpectedStatusCode.Value} " +
                            $"but got {statusCode}");
                    }
                }

                // 2. Validate Response Body Contains
                if (!string.IsNullOrWhiteSpace(testCase.ExpectedBodyContains))
                {
                    if (!responseBody.Contains(
                        testCase.ExpectedBodyContains,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add(
                            $"Response body does not contain '{testCase.ExpectedBodyContains}'");
                    }
                }

                // 3. Validate Response Time
                if (testCase.ExpectedResponseTimeMs.HasValue)
                {
                    if (stepTimer.ElapsedMilliseconds >= testCase.ExpectedResponseTimeMs.Value)
                    {
                        failures.Add(
                            $"Response time {stepTimer.ElapsedMilliseconds}ms " +
                            $"exceeds threshold {testCase.ExpectedResponseTimeMs.Value}ms");
                    }
                }

                // Determine Pass / Fail
                if (failures.Any())
                {
                    status = StatusFail;
                    failureReason = string.Join("; ", failures);
                    _logger.LogWarning(
                        "  ✗ FAIL [{Order}] {Method} {Url} → {Code} in {Ms}ms | {Reason}",
                        mapping.Order, mapping.Method, fullUrl,
                        statusCode, stepTimer.ElapsedMilliseconds, failureReason);
                }
                else
                {
                    status = StatusPass;
                    _logger.LogInformation(
                        "  ✓ PASS [{Order}] {Method} {Url} → {Code} in {Ms}ms",
                        mapping.Order, mapping.Method, fullUrl,
                        statusCode, stepTimer.ElapsedMilliseconds);
                }
            }
            catch (UriFormatException ex)
            {
                stepTimer.Stop();
                errorMessage = $"Invalid URL '{fullUrl}': {ex.Message}";
                _logger.LogError(
                    "  ✗ INVALID URL [{Order}] {Url}: {Msg}",
                    mapping.Order, fullUrl, ex.Message);
            }
            catch (Exception ex)
            {
                stepTimer.Stop();
                errorMessage = ex.Message;
                _logger.LogError(ex,
                    "  ✗ ERROR [{Order}] {Method} {Url}",
                    mapping.Order, mapping.Method, fullUrl);
            }

            var result = BuildStepResult(
                resultId, batchId, mapping, fullUrl,
                status, statusCode, responseBody,
                startedAt, DateTime.UtcNow,
                (int)stepTimer.ElapsedMilliseconds,
                errorMessage, failureReason);

            // Store step result in DB
            await SaveStepResultAsync(result, batchId, mapping, userId);

            return result;
        }

        /// <summary>
        /// Builds HttpRequestMessage from TestCase.
        /// Uses environment GlobalHeaders + test case RequestHeaders.
        /// Uses IHttpClientFactory — NOT new HttpClient().
        /// </summary>
        private static HttpRequestMessage BuildRequest(
            Domain.Entities.TestCase testCase,
            string fullUrl,
            Domain.Entities.Environment environment)
        {
            var method = new System.Net.Http.HttpMethod(
                testCase.Method.ToUpperInvariant());
            var request = new HttpRequestMessage(method, fullUrl);

            // Add environment global headers
            foreach (var (key, value) in environment.GlobalHeaders)
                request.Headers.TryAddWithoutValidation(key, value);

            // Add test case specific headers (override env headers)
            if (!string.IsNullOrWhiteSpace(testCase.RequestHeaders))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<
                        Dictionary<string, string>>(testCase.RequestHeaders);

                    if (headers != null)
                        foreach (var (key, value) in headers)
                        {
                            if (string.Equals(key, "Content-Type",
                                StringComparison.OrdinalIgnoreCase))
                                continue; // set with body below
                            request.Headers.TryAddWithoutValidation(key, value);
                        }
                }
                catch { /* invalid headers JSON — skip */ }
            }

            // Add request body for methods that support it
            var httpMethod = testCase.Method.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(testCase.RequestBody)
                && httpMethod is "POST" or "PUT" or "PATCH")
            {
                request.Content = new StringContent(
                    testCase.RequestBody,
                    Encoding.UTF8,
                    "application/json");
            }

            return request;
        }

        private static ExecutionStepResultDto BuildStepResult(
            Guid resultId,
            Guid batchId,
            SuiteTestCaseMappingDto mapping,
            string fullUrl,
            string status,
            int? statusCode,
            string? responseBody,
            DateTime startedAt,
            DateTime completedAt,
            int durationMs,
            string? errorMessage,
            string? failureReason) => new()
            {
                Id = resultId,
                BatchId = batchId,
                TestCaseId = mapping.TestCaseId,
                TestCaseTitle = mapping.TestCaseTitle,
                Endpoint = mapping.Endpoint,
                Method = mapping.Method,
                Order = mapping.Order,
                Status = status,
                StatusCode = statusCode,
                ResponseTimeMs = durationMs,
                ErrorMessage = errorMessage,
                FailureReason = failureReason,
                FullUrl = fullUrl,
                HttpMethod = mapping.Method,
                ResponseBody = responseBody,
                StartedAt = startedAt,
                CompletedAt = completedAt
            };

        private async Task SaveStepResultAsync(
            ExecutionStepResultDto result,
            Guid batchId,
            SuiteTestCaseMappingDto mapping,
            Guid userId)
        {
            try
            {
                // Map to domain ExecutionResult for storage
                var entity = new Domain.Entities.ExecutionResult
                {
                    Id = result.Id,
                    BatchId = batchId,
                    TestSuiteId = Guid.Empty, // populated from batch in full implementation
                    TestCaseId = mapping.TestCaseId,
                    TestStepId = Guid.Empty,
                    Status = result.Status == StatusPass
                        ? Domain.Enums.StepStatus.Passed
                        : result.Status == StatusFail
                            ? Domain.Enums.StepStatus.Failed
                            : Domain.Enums.StepStatus.Error,
                    DurationMs = result.ResponseTimeMs ?? 0,
                    ErrorMessage = result.ErrorMessage ?? result.FailureReason,
                    ExecutedAt = result.StartedAt,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _executionRepository.SaveStepResultAsync(
                    entity, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Never abort suite execution due to storage failure
                _logger.LogError(ex,
                    "Failed to save step result for TestCase {TestCaseId}",
                    mapping.TestCaseId);
            }
        }

        private static Domain.Entities.ExecutionBatch BuildBatch(
            Guid batchId, Guid suiteId, Guid projectId,
            Guid envId, bool failFast, bool parallel, Guid userId) =>
            new()
            {
                Id = batchId,
                TestSuiteId = suiteId,
                ProjectId = projectId,
                EnvironmentId = envId,
                Status = Domain.Enums.ExecutionStatus.Running,
                Trigger = Domain.Enums.ExecutionTrigger.Manual,
                FailFast = failFast,
                ParallelCases = parallel,
                StartedAt = DateTime.UtcNow,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
    }
}
