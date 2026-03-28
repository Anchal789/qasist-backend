using static QAsist.Application.DTOs.SuiteMappingDtos;

namespace QAsist.Application.Interfaces.IServices
{
    /// <summary>
    /// Application service for TestSuite ↔ TestCase mapping.
    ///
    /// Clean Architecture dependency rule enforced:
    ///   Api.Controllers → ITestSuiteMappingService (Application interface)
    ///   Never: Api.Controllers → ITestSuiteMappingRepository (Infrastructure)
    /// </summary>
    public interface ITestSuiteMappingService
    {
        Task<IEnumerable<SuiteTestCaseMappingDto>> GetMappedTestCasesAsync(
            Guid suiteId,
            CancellationToken cancellationToken = default);

        Task<AddTestCasesResultDto> AddTestCasesAsync(
            Guid suiteId,
            AddTestCasesToSuiteDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task RemoveMappingAsync(
            Guid suiteId,
            Guid mappingId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task ReorderAsync(
            Guid suiteId,
            ReorderSuiteTestCasesDto dto,
            CancellationToken cancellationToken = default);
    }
}