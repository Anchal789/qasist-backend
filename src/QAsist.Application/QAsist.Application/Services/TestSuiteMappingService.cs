using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Application.Services
{
    /// <summary>
    /// Application service for TestSuite ↔ TestCase mapping.
    ///
    /// Dependencies: only Application interfaces (ITestSuiteMappingRepository,
    /// ITestSuiteRepository, ITestCaseRepository) — all defined in Application layer.
    /// Infrastructure implementations injected at runtime via DI.
    /// </summary>
    public class TestSuiteMappingService : ITestSuiteMappingService
    {
        private readonly ITestSuiteMappingRepository _mappingRepository;
        private readonly ITestSuiteRepository _suiteRepository;
        private readonly ITestCaseRepository _testCaseRepository;
        private readonly ILogger<TestSuiteMappingService> _logger;

        public TestSuiteMappingService(
            ITestSuiteMappingRepository mappingRepository,
            ITestSuiteRepository suiteRepository,
            ITestCaseRepository testCaseRepository,
            ILogger<TestSuiteMappingService> logger)
        {
            _mappingRepository = mappingRepository;
            _suiteRepository = suiteRepository;
            _testCaseRepository = testCaseRepository;
            _logger = logger;
        }

        // ── GET MAPPED TEST CASES ─────────────────────────────────────────────
        public async Task<IEnumerable<SuiteTestCaseMappingDto>> GetMappedTestCasesAsync(
            Guid suiteId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching mapped test cases for suite {SuiteId}", suiteId);

                await EnsureSuiteExistsAsync(suiteId, cancellationToken);

                var mappings = await _mappingRepository
                    .GetBySuiteWithDetailsAsync(suiteId, cancellationToken);

                var result = mappings.ToList();

                _logger.LogInformation(
                    "Fetched {Count} mapped test cases for suite {SuiteId}",
                    result.Count, suiteId);

                return result;
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching mapped test cases for suite {SuiteId}", suiteId);
                throw;
            }
        }

        // ── ADD TEST CASES ────────────────────────────────────────────────────
        public async Task<AddTestCasesResultDto> AddTestCasesAsync(
            Guid suiteId,
            AddTestCasesToSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Adding {Count} test cases to suite {SuiteId} by user {UserId}",
                    dto.TestCases.Count, suiteId, userId);

                // 1. Validate suite exists
                await EnsureSuiteExistsAsync(suiteId, cancellationToken);

                // 2. Validate no duplicate IDs within the request
                var duplicateIds = dto.TestCases
                    .GroupBy(m => m.TestCaseId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateIds.Any())
                    throw new ValidationException(
                        $"Duplicate TestCaseIds in request: {string.Join(", ", duplicateIds)}");

                // 3. Validate all TestCase IDs exist in DB
                foreach (var mapping in dto.TestCases)
                {
                    var exists = await _testCaseRepository
                        .ExistsAsync(mapping.TestCaseId, cancellationToken);
                    if (!exists)
                        throw new NotFoundException(
                            $"TestCase {mapping.TestCaseId} not found.");
                }

                // 4. Count how many are already mapped (to report skipped)
                int alreadyMapped = 0;
                foreach (var mapping in dto.TestCases)
                {
                    if (await _mappingRepository.ExistsAsync(
                        suiteId, mapping.TestCaseId, cancellationToken))
                        alreadyMapped++;
                }

                // 5. Add (ON CONFLICT DO NOTHING for duplicates)
                var createdIds = (await _mappingRepository.AddRangeAsync(
                    suiteId, dto.TestCases, userId, cancellationToken)).ToList();

               

                return new AddTestCasesResultDto
                {
                    AddedCount = createdIds.Count,
                    MappingIds = createdIds,
                    SkippedCount = alreadyMapped
                };
            }
            catch (NotFoundException) { throw; }
            catch (ValidationException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error adding test cases to suite {SuiteId}", suiteId);
                throw;
            }
        }

        // ── REMOVE MAPPING ────────────────────────────────────────────────────
        public async Task RemoveMappingAsync(
            Guid suiteId,
            Guid mappingId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Removing mapping {MappingId} from suite {SuiteId}", mappingId, suiteId);

                await EnsureSuiteExistsAsync(suiteId, cancellationToken);

                var removed = await _mappingRepository
                    .RemoveAsync(mappingId, userId, cancellationToken);

                if (!removed)
                    throw new NotFoundException(
                        $"Mapping {mappingId} not found in suite {suiteId}.");

                _logger.LogInformation(
                    "Removed mapping {MappingId} from suite {SuiteId}", mappingId, suiteId);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error removing mapping {MappingId} from suite {SuiteId}",
                    mappingId, suiteId);
                throw;
            }
        }

        // ── REORDER ───────────────────────────────────────────────────────────
        public async Task ReorderAsync(
            Guid suiteId,
            ReorderSuiteTestCasesDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Reordering {Count} test cases in suite {SuiteId}",
                    dto.Order.Count, suiteId);

                await EnsureSuiteExistsAsync(suiteId, cancellationToken);

                await _mappingRepository.ReorderAsync(
                    suiteId, dto.Order, cancellationToken);

                _logger.LogInformation(
                    "Reordered test cases in suite {SuiteId}", suiteId);
            }
            catch (NotFoundException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error reordering suite {SuiteId}", suiteId);
                throw;
            }
        }

        // ── PRIVATE ───────────────────────────────────────────────────────────
        private async Task EnsureSuiteExistsAsync(
            Guid suiteId, CancellationToken ct)
        {
            var exists = await _suiteRepository.ExistsAsync(suiteId, ct);
            if (!exists)
                throw new NotFoundException(ResponseMessages.TestSuiteNotFound);
        }
    }

    /// <summary>Simple validation exception for business rule violations.</summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}