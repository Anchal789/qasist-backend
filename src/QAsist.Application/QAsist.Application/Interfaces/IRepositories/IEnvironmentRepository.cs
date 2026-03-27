using QAsist.Domain.Entities;
using Environment = QAsist.Domain.Entities.Environment;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface IEnvironmentRepository
    {
        Task<Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IEnumerable<Environment>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default);

        Task<Environment?> GetByNameAsync(
            Guid projectId,
            string name,
            CancellationToken cancellationToken = default);

        Task<Guid> CreateAsync(
            Environment environment,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            Environment environment,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}