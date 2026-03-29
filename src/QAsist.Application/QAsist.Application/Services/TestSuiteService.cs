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
    /// <summary>
    /// FIX: Merged partial class back into one complete class that
    /// properly implements ITestSuiteService — fixes CS0311 DI error.
    ///
    /// Also fixes:
    ///   CS0266 — RequestLog/ResponseLog cast: ExecutionResult.RequestLog
    ///            is typed as RequestLog?, not object.
    /// </summary>
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
            _logger.LogInformation("Starting {MethodName} — SuiteId={Id}", nameof(GetByIdAsync), id);
            try
            {
                var suite = await _suiteRepository.GetByIdAsync(id, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                _logger.LogInformation("Completed {MethodName} — SuiteId={Id}", nameof(GetByIdAsync), id);
                return MapToDto(suite);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName} — SuiteId={Id}", nameof(GetByIdAsync), id);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(GetByIdAsync));
            }
        }

        // ── GET WITH FULL TREE ────────────────────────────────────────────────
        public async Task<TestSuiteDetailDto> GetWithDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} — SuiteId={Id}", nameof(GetWithDetailsAsync), id);
            try
            {
                var suite = await _suiteRepository.GetWithDetailsAsync(id, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                _logger.LogInformation("Completed {MethodName} — Cases={Cases}, Steps={Steps}",
                    nameof(GetWithDetailsAsync),
                    suite.TestCases.Count,
                    suite.TestCases.Sum(c => c.Steps.Count));

                return MapToDetailDto(suite);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName} — SuiteId={Id}", nameof(GetWithDetailsAsync), id);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(GetWithDetailsAsync));
            }
        }

        // ── GET BY PROJECT ────────────────────────────────────────────────────
        public async Task<IEnumerable<TestSuiteDto>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} — ProjectId={ProjectId}", nameof(GetByProjectAsync), projectId);
            try
            {
                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var suites = await _suiteRepository.GetByProjectAsync(projectId, cancellationToken);
                var result = suites.Select(MapToDto).ToList();

                _logger.LogInformation("Completed {MethodName} — Count={Count}", nameof(GetByProjectAsync), result.Count);
                return result;
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName}", nameof(GetByProjectAsync));
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(GetByProjectAsync));
            }
        }

        // ── GET PAGED ─────────────────────────────────────────────────────────
        public async Task<(IEnumerable<TestSuiteDto> Suites, int TotalCount)> GetPagedAsync(
            Guid projectId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} — ProjectId={ProjectId}, Page={Page}",
                nameof(GetPagedAsync), projectId, pageNumber);
            try
            {
                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var (suites, total) = await _suiteRepository.GetPagedAsync(
                    projectId, pageNumber, pageSize, cancellationToken);

                var dtos = suites.Select(MapToDto).ToList();

                _logger.LogInformation("Completed {MethodName} — Page={Page}, Count={Count}, Total={Total}",
                    nameof(GetPagedAsync), pageNumber, dtos.Count, total);

                return (dtos, total);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName}", nameof(GetPagedAsync));
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(GetPagedAsync));
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<TestSuiteDto> CreateAsync(
            CreateTestSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} — Name={Name}, ProjectId={ProjectId}",
                nameof(CreateAsync), dto.Name, dto.ProjectId);
            try
            {
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

                var createdId = await _suiteRepository.CreateAsync(suite, userId, cancellationToken);
                suite.Id = createdId;

                _logger.LogInformation("Completed {MethodName} — SuiteId={SuiteId}", nameof(CreateAsync), createdId);
                return MapToDto(suite);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(CreateAsync));
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<TestSuiteDto> UpdateAsync(
            UpdateTestSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} — SuiteId={Id}", nameof(UpdateAsync), dto.Id);
            try
            {
                var existing = await _suiteRepository.GetByIdAsync(dto.Id, cancellationToken);
                if (existing is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                existing.Name = dto.Name.Trim();
                existing.Description = dto.Description?.Trim();
                existing.Variables = dto.Variables;
                existing.ParallelCases = dto.ParallelCases;
                existing.IsActive = dto.IsActive;

                await _suiteRepository.UpdateAsync(existing, userId, cancellationToken);

                var updated = await _suiteRepository.GetByIdAsync(dto.Id, cancellationToken);

                _logger.LogInformation("Completed {MethodName} — SuiteId={Id}", nameof(UpdateAsync), dto.Id);
                return MapToDto(updated!);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName} — SuiteId={Id}", nameof(UpdateAsync), dto.Id);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(UpdateAsync));
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task DeleteAsync(
            Guid id, Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} — SuiteId={Id}", nameof(DeleteAsync), id);
            try
            {
                var exists = await _suiteRepository.ExistsAsync(id, cancellationToken);
                if (!exists)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                await _suiteRepository.DeleteAsync(id, userId, cancellationToken);

                _logger.LogInformation("Completed {MethodName} — SuiteId={Id} soft-deleted", nameof(DeleteAsync), id);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName} — SuiteId={Id}", nameof(DeleteAsync), id);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(DeleteAsync));
            }
        }

        // ── EXECUTE ───────────────────────────────────────────────────────────
        public async Task<ExecuteSuiteResponseDto> ExecuteAsync(
            Guid suiteId,
            ExecuteSuiteRequestDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting {MethodName} with data: {@Request}",
                nameof(ExecuteAsync),
                new { SuiteId = suiteId, dto.EnvironmentId, UserId = userId, dto.FailFast, dto.ParallelCases });
            try
            {
                // FIX: must use GetWithDetailsAsync — GetByIdAsync does NOT load TestCases/Steps
                var suite = await _suiteRepository.GetWithDetailsAsync(suiteId, cancellationToken);
                if (suite is null)
                    throw new NotFoundException(ResponseMessages.TestSuiteNotFound);

                if (suite.TestCases == null || !suite.TestCases.Any())
                {
                    _logger.LogError("{MethodName}: Suite {SuiteId} has 0 test cases", nameof(ExecuteAsync), suiteId);
                    throw new InvalidOperationException(
                        $"Suite '{suite.Name}' has no test cases. Add test cases before executing.");
                }

                int totalExpectedSteps = suite.TestCases
                    .Sum(tc => tc.Steps.Count(s => s.IsEnabled));

                _logger.LogInformation("{MethodName}: {CaseCount} cases, {StepCount} steps",
                    nameof(ExecuteAsync), suite.TestCases.Count, totalExpectedSteps);

                if (totalExpectedSteps == 0)
                {
                    _logger.LogError("{MethodName}: 0 steps found — aborting", nameof(ExecuteAsync));
                    throw new InvalidOperationException(
                        "No executable steps found. Add steps to your test cases.");
                }

                var environment = await _environmentRepository.GetByIdAsync(
                    dto.EnvironmentId, cancellationToken);
                if (environment is null)
                    throw new NotFoundException(ResponseMessages.EnvironmentNotFound);

                var envVars = new Dictionary<string, string>(environment.GlobalHeaders)
                {
                    ["baseUrl"] = environment.BaseUrl
                };

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

                _logger.LogInformation("Batch {BatchId} created — starting background execution", batchId);

                _ = Task.Run(async () =>
                {
                    _logger.LogInformation("Background execution started — BatchId={BatchId}", batchId);
                    try
                    {
                        var options = new ExecutionOptions
                        {
                            BatchId = batchId,
                            TenantId = userId,
                            EnvironmentId = dto.EnvironmentId,
                            EnvironmentVariables = envVars,
                            FailFast = dto.FailFast,
                            ParallelCases = dto.ParallelCases
                        };

                        var result = await _suiteExecutor.ExecuteAsync(
                            suite, options, CancellationToken.None);

                        // FIX: persist each step result
                        foreach (var stepResult in result.StepResults)
                        {
                            // FIX CS0266: RequestLog and ResponseLog must be the
                            // correct typed value objects, not raw object.
                            RequestLog? requestLog = null;
                            ResponseLog? responseLog = null;

                            if (stepResult.RequestLog is RequestLog rl)
                                requestLog = rl;
                            else if (stepResult.RequestLog != null)
                            {
                                // SuiteExecutor stores as anonymous object — re-map
                                var json = System.Text.Json.JsonSerializer
                                    .Serialize(stepResult.RequestLog);
                                requestLog = System.Text.Json.JsonSerializer
                                    .Deserialize<RequestLog>(json);
                            }

                            if (stepResult.ResponseLog is ResponseLog rsl)
                                responseLog = rsl;
                            else if (stepResult.ResponseLog != null)
                            {
                                var json = System.Text.Json.JsonSerializer
                                    .Serialize(stepResult.ResponseLog);
                                responseLog = System.Text.Json.JsonSerializer
                                    .Deserialize<ResponseLog>(json);
                            }

                            var entity = new ExecutionResult
                            {
                                Id = Guid.NewGuid(),
                                BatchId = batchId,
                                TestSuiteId = suiteId,
                                TestCaseId = stepResult.TestCaseId,
                                TestStepId = stepResult.TestStepId,
                                Status = stepResult.Status,
                                DurationMs = stepResult.DurationMs,
                                ErrorMessage = stepResult.ErrorMessage,
                                RequestLog = requestLog,       // typed correctly
                                ResponseLog = responseLog,     // typed correctly
                                AssertionResults = stepResult.AssertionResults,
                                ExtractedVariables = stepResult.ExtractedVariables
                                    .ToDictionary(k => k.Key, v => v.Value),
                                ExecutedAt = DateTime.UtcNow,
                                CreatedBy = userId,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _executionRepository.SaveStepResultAsync(
                                entity, CancellationToken.None);
                        }

                        batch.Status = result.IsSuccess
                            ? ExecutionStatus.Completed
                            : ExecutionStatus.Failed;
                        batch.TotalSteps = result.TotalSteps;
                        batch.PassedSteps = result.PassedSteps;
                        batch.FailedSteps = result.FailedSteps;
                        batch.SkippedSteps = result.SkippedSteps;
                        batch.ErrorSteps = result.ErrorSteps;
                        batch.CompletedAt = DateTime.UtcNow;

                        await _executionRepository.UpdateBatchAsync(batch, CancellationToken.None);

                        _logger.LogInformation(
                            "Background completed — BatchId={BatchId}, Total={T}, Pass={P}, Fail={F}, Pct={Pct}%",
                            batchId, result.TotalSteps, result.PassedSteps,
                            result.FailedSteps, result.PassPercentage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background execution failed — BatchId={BatchId}", batchId);
                        batch.Status = ExecutionStatus.Failed;
                        batch.CompletedAt = DateTime.UtcNow;
                        await _executionRepository.UpdateBatchAsync(batch, CancellationToken.None);
                    }
                    finally
                    {
                        _logger.LogInformation("Finished background execution — BatchId={BatchId}", batchId);
                    }
                }, cancellationToken);

                _logger.LogInformation("Completed {MethodName} — BatchId={BatchId} dispatched",
                    nameof(ExecuteAsync), batchId);

                return new ExecuteSuiteResponseDto
                {
                    BatchId = batchId,
                    Status = ExecutionStatus.Running.ToString(),
                    StartedAt = batch.StartedAt!.Value,
                    Message = ResponseMessages.ExecutionStarted
                };
            }
            catch (NotFoundException) { throw; }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName} — SuiteId={SuiteId}", nameof(ExecuteAsync), suiteId);
                throw;
            }
            finally
            {
                _logger.LogInformation("Finished {MethodName}", nameof(ExecuteAsync));
            }
        }

        // ── PRIVATE: Helpers ──────────────────────────────────────────────────
        private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken ct)
        {
            var project = await _projectRepository.GetByIdAsync(projectId, ct);
            if (project is null)
            {
                _logger.LogWarning("Project {ProjectId} not found", projectId);
                throw new NotFoundException(ResponseMessages.ProjectNotFound);
            }
        }

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