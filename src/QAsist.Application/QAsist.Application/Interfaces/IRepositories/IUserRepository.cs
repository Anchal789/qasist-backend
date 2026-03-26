using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Guid> CreateAsync(User user, Guid createdBy, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(User user, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
        Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(User user);
    }
}
