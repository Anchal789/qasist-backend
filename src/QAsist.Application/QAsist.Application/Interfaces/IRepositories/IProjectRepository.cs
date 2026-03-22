using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<(IEnumerable<Project> Projects, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Guid> CreateAsync(Project project, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Project project, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    }
}
