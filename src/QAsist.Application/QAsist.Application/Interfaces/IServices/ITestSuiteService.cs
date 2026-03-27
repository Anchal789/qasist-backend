using static QAsist.Application.DTOs.Enginedtos;

namespace QAsist.Application.Interfaces.IServices
{
    public interface ITestSuiteService
    {
        Task<TestSuiteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TestSuiteDetailDto> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<TestSuiteDto>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<TestSuiteDto> Suites, int TotalCount)> GetPagedAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<TestSuiteDto> CreateAsync(CreateTestSuiteDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<TestSuiteDto> UpdateAsync(UpdateTestSuiteDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<ExecuteSuiteResponseDto> ExecuteAsync(Guid suiteId, ExecuteSuiteRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
    }
}
