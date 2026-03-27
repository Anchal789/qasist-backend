using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Services
{
    public class TestCaseService : ITestCaseService
    {
        private readonly ITestCaseRepository _testCaseRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<TestCaseService> _logger;

        public TestCaseService(
            ITestCaseRepository testCaseRepository,
            IProjectRepository projectRepository,
            ILogger<TestCaseService> logger)
        {
            _testCaseRepository = testCaseRepository;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<TestCaseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching test case {TestCaseId}", id);

                var testCase = await _testCaseRepository.GetByIdAsync(id, cancellationToken);
                if (testCase is null)
                    throw new NotFoundException(ResponseMessages.TestCaseNotFound);

                _logger.LogInformation("Successfully fetched test case {TestCaseId}", id);
                return MapToDto(testCase);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching test case {TestCaseId}", id);
                throw;
            }
        }

        // ── GET BY PROJECT ────────────────────────────────────────────────────
        public async Task<IEnumerable<TestCaseSummaryDto>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching test cases for project {ProjectId}", projectId);

                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var testCases = await _testCaseRepository.GetByProjectAsync(projectId, cancellationToken);
                var result = testCases.Select(MapToSummaryDto).ToList();

                _logger.LogInformation(
                    "Fetched {Count} test cases for project {ProjectId}", result.Count, projectId);
                return result;
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching test cases for project {ProjectId}", projectId);
                throw;
            }
        }

        // ── GET PAGED ─────────────────────────────────────────────────────────
        public async Task<(IEnumerable<TestCaseSummaryDto> TestCases, int TotalCount)> GetPagedAsync(
            Guid projectId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Paged test cases for project {ProjectId}. Page={Page} Size={Size}",
                    projectId, pageNumber, pageSize);

                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var (testCases, totalCount) = await _testCaseRepository.GetPagedAsync(
                    projectId, pageNumber, pageSize, cancellationToken);

                var dtos = testCases.Select(MapToSummaryDto).ToList();

                _logger.LogInformation(
                    "Page {Page}: {Count}/{Total} test cases for project {ProjectId}",
                    pageNumber, dtos.Count, totalCount, projectId);

                return (dtos, totalCount);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPagedAsync for project {ProjectId}", projectId);
                throw;
            }
        }

        // ── GET BY STATUS ─────────────────────────────────────────────────────
        public async Task<IEnumerable<TestCaseSummaryDto>> GetByStatusAsync(
            Guid projectId,
            TestCaseStatus status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching {Status} test cases for project {ProjectId}", status, projectId);

                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var testCases = await _testCaseRepository.GetByStatusAsync(projectId, status, cancellationToken);
                return testCases.Select(MapToSummaryDto);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetByStatusAsync {ProjectId} status={Status}", projectId, status);
                throw;
            }
        }

        // ── GET AI GENERATED ──────────────────────────────────────────────────
        public async Task<IEnumerable<TestCaseSummaryDto>> GetAiGeneratedAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching AI test cases for project {ProjectId}", projectId);
                await EnsureProjectExistsAsync(projectId, cancellationToken);
                var testCases = await _testCaseRepository.GetAiGeneratedAsync(projectId, cancellationToken);
                return testCases.Select(MapToSummaryDto);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAiGeneratedAsync {ProjectId}", projectId);
                throw;
            }
        }

        // ── GET BY ASSIGNEE ───────────────────────────────────────────────────
        public async Task<IEnumerable<TestCaseSummaryDto>> GetByAssigneeAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching test cases for assignee {UserId}", userId);
                var testCases = await _testCaseRepository.GetByAssigneeAsync(userId, cancellationToken);
                return testCases.Select(MapToSummaryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByAssigneeAsync {UserId}", userId);
                throw;
            }
        }

        // ── SEARCH ────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TestCaseSummaryDto>> SearchAsync(
            Guid projectId,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Searching project {ProjectId} for '{Term}'", projectId, searchTerm);

                await EnsureProjectExistsAsync(projectId, cancellationToken);

                var testCases = await _testCaseRepository.SearchAsync(projectId, searchTerm, cancellationToken);
                var result = testCases.Select(MapToSummaryDto).ToList();

                _logger.LogInformation(
                    "Search '{Term}' returned {Count} results in project {ProjectId}",
                    searchTerm, result.Count, projectId);

                return result;
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchAsync project {ProjectId}", projectId);
                throw;
            }
        }

        // ── STATISTICS ────────────────────────────────────────────────────────
        public async Task<TestCaseStatisticsDto> GetStatisticsAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching statistics for project {ProjectId}", projectId);
                await EnsureProjectExistsAsync(projectId, cancellationToken);
                return await _testCaseRepository.GetStatisticsAsync(projectId, cancellationToken);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStatisticsAsync {ProjectId}", projectId);
                throw;
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<TestCaseDto> CreateAsync(
            CreateTestCaseDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Creating manual test case '{Title}' for project {ProjectId} by user {UserId}",
                    dto.Title, dto.ProjectId, userId);

                await EnsureProjectExistsAsync(dto.ProjectId, cancellationToken);

                var testCase = new TestCase
                {
                    Id = Guid.NewGuid(),
                    ProjectId = dto.ProjectId,
                    Endpoint = dto.Endpoint.Trim(),
                    Method = dto.Method.ToUpperInvariant(),
                    Title = dto.Title.Trim(),
                    Steps = dto.Steps,
                    ExpectedResult = dto.ExpectedResult.Trim(),  // NOT NULL — safe to trim
                    Priority = dto.Priority,
                    Status = dto.Status,
                    RequestHeaders = dto.RequestHeaders,
                    RequestBody = dto.RequestBody,
                    ExpectedStatusCode = dto.ExpectedStatusCode,
                    ExpectedBodyContains = dto.ExpectedBodyContains,
                    ExpectedResponseTimeMs = dto.ExpectedResponseTimeMs,
                    AssignedTo = dto.AssignedTo,
                    IsAiGenerated = false                        // always false for manual
                };

                var createdId = await _testCaseRepository.CreateAsync(testCase, userId, cancellationToken);
                testCase.Id = createdId;

                _logger.LogInformation(
                    "Created test case {TestCaseId} '{Title}' for project {ProjectId}",
                    createdId, dto.Title, dto.ProjectId);

                return MapToDto(testCase);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating test case '{Title}' for project {ProjectId}", dto.Title, dto.ProjectId);
                throw;
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<TestCaseDto> UpdateAsync(
            UpdateTestCaseDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating test case {TestCaseId} by user {UserId}", dto.Id, userId);

                var existing = await _testCaseRepository.GetByIdAsync(dto.Id, cancellationToken);
                if (existing is null)
                    throw new NotFoundException(ResponseMessages.TestCaseNotFound);

                // Update only mutable fields — preserve ProjectId, IsAiGenerated, CreatedBy, CreatedAt
                existing.Endpoint = dto.Endpoint.Trim();
                existing.Method = dto.Method.ToUpperInvariant();
                existing.Title = dto.Title.Trim();
                existing.Steps = dto.Steps;
                existing.ExpectedResult = dto.ExpectedResult.Trim();
                existing.Priority = dto.Priority;
                existing.Status = dto.Status;
                existing.RequestHeaders = dto.RequestHeaders;
                existing.RequestBody = dto.RequestBody;
                existing.ExpectedStatusCode = dto.ExpectedStatusCode;
                existing.ExpectedBodyContains = dto.ExpectedBodyContains;
                existing.ExpectedResponseTimeMs = dto.ExpectedResponseTimeMs;
                existing.AssignedTo = dto.AssignedTo;

                await _testCaseRepository.UpdateAsync(existing, userId, cancellationToken);

                // Re-fetch so UpdatedAt is accurate from DB
                var updated = await _testCaseRepository.GetByIdAsync(dto.Id, cancellationToken);

                _logger.LogInformation("Updated test case {TestCaseId}", dto.Id);
                return MapToDto(updated!);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test case {TestCaseId}", dto.Id);
                throw;
            }
        }

        // ── UPDATE STATUS ─────────────────────────────────────────────────────
        public async Task UpdateStatusAsync(
            Guid id,
            UpdateTestCaseStatusDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Updating status of test case {TestCaseId} to {Status}", id, dto.Status);

                await EnsureTestCaseExistsAsync(id, cancellationToken);
                await _testCaseRepository.UpdateStatusAsync(id, dto.Status, userId, cancellationToken);

                _logger.LogInformation("Status of {TestCaseId} updated to {Status}", id, dto.Status);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusAsync {TestCaseId}", id);
                throw;
            }
        }

        // ── ASSIGN ────────────────────────────────────────────────────────────
        public async Task AssignAsync(
            Guid id,
            AssignTestCaseDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Assigning test case {TestCaseId} to user {AssignedTo}", id, dto.AssignedTo);

                await EnsureTestCaseExistsAsync(id, cancellationToken);
                await _testCaseRepository.AssignAsync(id, dto.AssignedTo, userId, cancellationToken);

                _logger.LogInformation(
                    "Test case {TestCaseId} assigned to {AssignedTo}", id, dto.AssignedTo);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignAsync {TestCaseId}", id);
                throw;
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting test case {TestCaseId} by user {UserId}", id, userId);

                await EnsureTestCaseExistsAsync(id, cancellationToken);
                await _testCaseRepository.DeleteAsync(id, userId, cancellationToken);

                _logger.LogInformation("Test case {TestCaseId} soft-deleted by user {UserId}", id, userId);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAsync {TestCaseId}", id);
                throw;
            }
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken ct)
        {
            var project = await _projectRepository.GetByIdAsync(projectId, ct);
            if (project is null)
                throw new NotFoundException(ResponseMessages.ProjectNotFound);
        }

        private async Task EnsureTestCaseExistsAsync(Guid id, CancellationToken ct)
        {
            var exists = await _testCaseRepository.ExistsAsync(id, ct);
            if (!exists)
                throw new NotFoundException(ResponseMessages.TestCaseNotFound);
        }

        // Maps entity → full DTO (GET by ID response)
        private static TestCaseDto MapToDto(TestCase tc) => new()
        {
            Id = tc.Id,
            ProjectId = tc.ProjectId,
            Endpoint = tc.Endpoint,
            Method = tc.Method,
            Title = tc.Title,
            Steps = tc.Steps,
            ExpectedResult = tc.ExpectedResult,
            Priority = tc.Priority.ToString(),   // "Low"/"Medium"/"High"/"Critical"
            Status = tc.Status.ToString(),     // "Draft"/"Active"/"Passed" etc.
            IsAiGenerated = tc.IsAiGenerated,
            CreatedAt = tc.CreatedAt,
            UpdatedAt = tc.UpdatedAt,
            AssignedTo = tc.AssignedTo,
            RequestHeaders = tc.RequestHeaders,
            RequestBody = tc.RequestBody,
            ExpectedStatusCode = tc.ExpectedStatusCode,
            ExpectedBodyContains = tc.ExpectedBodyContains,
            ExpectedResponseTimeMs = tc.ExpectedResponseTimeMs
        };

        // Maps entity → summary DTO (list/table views)
        private static TestCaseSummaryDto MapToSummaryDto(TestCase tc) => new()
        {
            Id = tc.Id,
            ProjectId = tc.ProjectId,
            Title = tc.Title,
            Method = tc.Method,
            Endpoint = tc.Endpoint,
            Priority = tc.Priority.ToString(),
            Status = tc.Status.ToString(),
            IsAiGenerated = tc.IsAiGenerated,
            AssignedTo = tc.AssignedTo,
            CreatedAt = tc.CreatedAt,
            UpdatedAt = tc.UpdatedAt
        };
    }
}