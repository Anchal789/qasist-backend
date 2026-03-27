using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Application.Services
{
    public class TestSuiteService : ITestSuiteService
    {
        private readonly ITestSuiteRepository _suiteRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IEnvironmentRepository _environmentRepository;
        private readonly IExecutionRepository _executionRepository;
        private readonly ISuiteExecutor _suiteExecutor;
        private readonly ILogger<TestSuiteService> _logger;

        public TestSuiteService(
            ITestSuiteRepository suiteRepository,
            IProjectRepository projectRepository,
            IEnvironmentRepository environmentRepository,
            IExecutionRepository executionRepository,
            ISuiteExecutor suiteExecutor,
            ILogger<TestSuiteService> logger)
        {
            _suiteRepository = suiteRepository;
            _projectRepository = projectRepository;
            _environmentRepository = environmentRepository;
            _executionRepository = executionRepository;
            _suiteExecutor = suiteExecutor;
            _logger = logger;
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<TestSuiteDto> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching test suite {SuiteId}", id);

                var suite = await _suiteRepository.GetByIdAsync(id, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                _logger.LogInformation("Fetched test suite {SuiteId}", id);
                return MapToDto(suite);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching test suite {SuiteId}", id);
                throw;
            }
        }

        // ── GET WITH FULL TREE ────────────────────────────────────────────────
        public async Task<TestSuiteDetailDto> GetWithDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching test suite {SuiteId} with full details", id);

                var suite = await _suiteRepository.GetWithDetailsAsync(id, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                _logger.LogInformation(
                    "Fetched suite {SuiteId} — {Cases} cases, {Steps} steps",
                    id,
                    suite.TestCases.Count,
                    suite.TestCases.Sum(c => c.Steps.Count));

                return MapToDetailDto(suite);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching suite details {SuiteId}", id);
                throw;
            }
        }

        // ── GET BY PROJECT ────────────────────────────────────────────────────
        public async Task<IEnumerable<TestSuiteDto>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching test suites for project {ProjectId}", projectId);

                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var suites = await _suiteRepository.GetByProjectAsync(
                    projectId, cancellationToken);

                var result = suites.Select(MapToDto).ToList();

                _logger.LogInformation(
                    "Fetched {Count} test suites for project {ProjectId}",
                    result.Count, projectId);

                return result;
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching suites for project {ProjectId}", projectId);
                throw;
            }
        }

        // ── GET PAGED ─────────────────────────────────────────────────────────
        public async Task<(IEnumerable<TestSuiteDto> Suites, int TotalCount)> GetPagedAsync(
            Guid projectId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching paged suites for project {ProjectId}. Page={Page} Size={Size}",
                    projectId, pageNumber, pageSize);

                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var (suites, total) = await _suiteRepository.GetPagedAsync(
                    projectId, pageNumber, pageSize, cancellationToken);

                var dtos = suites.Select(MapToDto).ToList();

                _logger.LogInformation(
                    "Page {Page}: {Count}/{Total} suites for project {ProjectId}",
                    pageNumber, dtos.Count, total, projectId);

                return (dtos, total);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching paged suites for project {ProjectId}", projectId);
                throw;
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<TestSuiteDto> CreateAsync(
            CreateTestSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Creating test suite '{Name}' for project {ProjectId} by user {UserId}",
                    dto.Name, dto.ProjectId, userId);

                await EnsureProjectExistsAsync(dto.ProjectId, cancellationToken);

                var suite = new TestSuite
                {
                    Id = Guid.NewGuid(),
                    ProjectId = dto.ProjectId,
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    Variables = dto.Variables,
                    ParallelCases = dto.ParallelCases,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdId = await _suiteRepository.CreateAsync(
                    suite, userId, cancellationToken);
                suite.Id = createdId;

                _logger.LogInformation(
                    "Created test suite {SuiteId} '{Name}' for project {ProjectId}",
                    createdId, dto.Name, dto.ProjectId);

                return MapToDto(suite);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating test suite '{Name}' for project {ProjectId}",
                    dto.Name, dto.ProjectId);
                throw;
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<TestSuiteDto> UpdateAsync(
            UpdateTestSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Updating test suite {SuiteId} by user {UserId}", dto.Id, userId);

                var existing = await _suiteRepository.GetByIdAsync(
                    dto.Id, cancellationToken);
                if (existing is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                existing.Name = dto.Name.Trim();
                existing.Description = dto.Description?.Trim();
                existing.Variables = dto.Variables;
                existing.ParallelCases = dto.ParallelCases;
                existing.IsActive = dto.IsActive;

                await _suiteRepository.UpdateAsync(existing, userId, cancellationToken);

                // Re-fetch for accurate UpdatedAt from DB
                var updated = await _suiteRepository.GetByIdAsync(
                    dto.Id, cancellationToken);

                _logger.LogInformation("Updated test suite {SuiteId}", dto.Id);
                return MapToDto(updated!);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test suite {SuiteId}", dto.Id);
                throw;
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task DeleteAsync(
            Guid id, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Deleting test suite {SuiteId} by user {UserId}", id, userId);

                var exists = await _suiteRepository.ExistsAsync(id, cancellationToken);
                if (!exists)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                await _suiteRepository.DeleteAsync(id, userId, cancellationToken);

                _logger.LogInformation(
                    "Soft-deleted test suite {SuiteId}", id);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting test suite {SuiteId}", id);
                throw;
            }
        }

        // ── EXECUTE ───────────────────────────────────────────────────────────
        public async Task<ExecuteSuiteResponseDto> ExecuteAsync(
            Guid suiteId,
            ExecuteSuiteRequestDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Executing suite {SuiteId} on env {EnvId} by user {UserId}. " +
                    "FailFast={FF} Parallel={P}",
                    suiteId, dto.EnvironmentId, userId,
                    dto.FailFast, dto.ParallelCases);

                // 1. Load suite with full tree
                var suite = await _suiteRepository.GetWithDetailsAsync(
                    suiteId, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                // 2. Load environment variables
                var environment = await _environmentRepository.GetByIdAsync(
                    dto.EnvironmentId, cancellationToken);
                if (environment is null)
                    throw new NotFoundException(ResponseMessages.EnvironmentNotFound);

                // 3. Build environment variable dictionary
                // Merge: base URL + global headers + any env-level vars
                var envVars = new Dictionary<string, string>(
                    environment.GlobalHeaders)
                {
                    ["baseUrl"] = environment.BaseUrl
                };

                // 4. Create execution batch record
                var batchId = Guid.NewGuid();
                var batch = new ExecutionBatch
                {
                    Id = batchId,
                    TestSuiteId = suiteId,
                    ProjectId = suite.ProjectId,
                    EnvironmentId = dto.EnvironmentId,
                    Status = ExecutionStatus.Running,
                    Trigger = ExecutionTrigger.Manual,
                    FailFast = dto.FailFast,
                    ParallelCases = dto.ParallelCases,
                    StartedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _executionRepository.CreateBatchAsync(batch, cancellationToken);

                _logger.LogInformation(
                    "Execution batch {BatchId} created for suite {SuiteId}",
                    batchId, suiteId);

                // 5. Execute the suite (fire and forget for large suites,
                //    or await for immediate response — using await here for simplicity;
                //    Week 10+ will move to Hangfire background jobs)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var options = new ExecutionOptions
                        {
                            BatchId = batchId,
                            TenantId = userId, // simplified — real app uses tenant from JWT
                            EnvironmentId = dto.EnvironmentId,
                            EnvironmentVariables = envVars,
                            FailFast = dto.FailFast,
                            ParallelCases = dto.ParallelCases
                        };

                        var result = await _suiteExecutor.ExecuteAsync(
                            suite, options, CancellationToken.None);

                        // Update batch with final results
                        batch.Status = result.IsSuccess
                            ? ExecutionStatus.Completed
                            : ExecutionStatus.Failed;
                        batch.TotalSteps = result.TotalSteps;
                        batch.PassedSteps = result.PassedSteps;
                        batch.FailedSteps = result.FailedSteps;
                        batch.SkippedSteps = result.SkippedSteps;
                        batch.ErrorSteps = result.ErrorSteps;
                        batch.CompletedAt = DateTime.UtcNow;

                        // Persist each step result
                        foreach (var stepResult in result.StepResults)
                        {
                            await _executionRepository.SaveStepResultAsync(
                                MapStepResultToEntity(stepResult, batch, userId),
                                CancellationToken.None);
                        }

                        await _executionRepository.UpdateBatchAsync(
                            batch, CancellationToken.None);

                        _logger.LogInformation(
                            "Suite {SuiteId} batch {BatchId} completed. " +
                            "Pass={P} Fail={F} Skip={S} Pass%={Pct}%",
                            suiteId, batchId,
                            result.PassedSteps, result.FailedSteps,
                            result.SkippedSteps, result.PassPercentage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Background execution failed for batch {BatchId}", batchId);

                        // Mark batch as failed
                        batch.Status = ExecutionStatus.Failed;
                        batch.CompletedAt = DateTime.UtcNow;
                        await _executionRepository.UpdateBatchAsync(
                            batch, CancellationToken.None);
                    }
                }, cancellationToken);

                // Return immediately — client polls for results
                return new ExecuteSuiteResponseDto
                {
                    BatchId = batchId,
                    Status = ExecutionStatus.Running.ToString(),
                    StartedAt = batch.StartedAt!.Value,
                    Message = ResponseMessages.ExecutionStarted
                };
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error starting execution for suite {SuiteId}", suiteId);
                throw;
            }
        }

        // ── PRIVATE: Helpers ──────────────────────────────────────────────────

        private async Task EnsureProjectExistsAsync(
            Guid projectId, CancellationToken ct)
        {
            var project = await _projectRepository.GetByIdAsync(projectId, ct);
            if (project is null)
                throw new NotFoundException(ResponseMessages.ProjectNotFound);
        }

        private static ExecutionResult MapStepResultToEntity(
            StepExecutionResult stepResult,
            ExecutionBatch batch,
            Guid userId) => new()
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                TestSuiteId = batch.TestSuiteId,
                TestCaseId = Guid.Empty,   // populated from step in full implementation
                TestStepId = stepResult.TestStepId,
                Status = stepResult.Status,
                DurationMs = stepResult.DurationMs,
                ErrorMessage = stepResult.ErrorMessage,
                RequestLog = stepResult.RequestLog,
                ResponseLog = stepResult.ResponseLog,
                AssertionResults = stepResult.AssertionResults.ToList(),
                ExtractedVariables = stepResult.ExtractedVariables
                                     .ToDictionary(k => k.Key, v => v.Value),
                ExecutedAt = DateTime.UtcNow,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

        // ── PRIVATE: Mappers ──────────────────────────────────────────────────

        private static TestSuiteDto MapToDto(TestSuite suite) => new()
        {
            Id = suite.Id,
            ProjectId = suite.ProjectId,
            Name = suite.Name,
            Description = suite.Description,
            Variables = suite.Variables,
            ParallelCases = suite.ParallelCases,
            IsActive = suite.IsActive,
            TotalCases = suite.TestCases?.Count ?? 0,
            CreatedAt = suite.CreatedAt,
            UpdatedAt = suite.UpdatedAt
        };

        private static TestSuiteDetailDto MapToDetailDto(TestSuite suite)
        {
            var dto = new TestSuiteDetailDto
            {
                Id = suite.Id,
                ProjectId = suite.ProjectId,
                Name = suite.Name,
                Description = suite.Description,
                Variables = suite.Variables,
                ParallelCases = suite.ParallelCases,
                IsActive = suite.IsActive,
                TotalCases = suite.TestCases?.Count ?? 0,
                CreatedAt = suite.CreatedAt,
                UpdatedAt = suite.UpdatedAt
            };

            dto.TestCases = suite.TestCases?.Select(c => new TestCaseSuiteDto
            {
                Id = c.Id,
                Name = c.Name,
                OrderIndex = c.OrderIndex,
                IsEnabled = c.IsEnabled,
                Tags = c.Tags,
                Steps = c.Steps?.Select(s => new TestStepDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    OrderIndex = s.OrderIndex,
                    Method = s.Method.ToString(),
                    Url = s.Url,
                    RequestHeaders = s.RequestHeaders,
                    RequestBody = s.RequestBody,
                    TimeoutMs = s.TimeoutMs,
                    RetryCount = s.RetryCount,
                    IsEnabled = s.IsEnabled,
                    Assertions = s.Assertions?.Select(a => new AssertionDto
                    {
                        Id = a.Id,
                        AssertionType = a.AssertionType.ToString(),
                        Field = a.Field,
                        Operator = a.Operator,
                        ExpectedValue = a.ExpectedValue,
                        OrderIndex = a.OrderIndex,
                        IsRequired = a.IsRequired
                    }).ToList() ?? new(),
                    Extractions = s.Extractions?.Select(e => new ExtractionDto
                    {
                        Id = e.Id,
                        VariableName = e.VariableName,
                        Source = e.Source.ToString(),
                        JsonPath = e.JsonPath,
                        HeaderName = e.HeaderName,
                        DefaultValue = e.DefaultValue
                    }).ToList() ?? new()
                }).ToList() ?? new()
            }).ToList() ?? new();

            return dto;
        }
    }
}