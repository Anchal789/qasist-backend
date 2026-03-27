using QAsist.Application.DTOs;

namespace QAsist.Application.Interfaces.IServices
{
    public interface IProjectService
    {
        Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<(IEnumerable<ProjectDto> Projects, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<ProjectDto> CreateAsync(CreateProjectDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<ProjectDto> UpdateAsync(UpdateProjectDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    }
}
