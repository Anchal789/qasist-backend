using QAsist.Domain.Entities;

namespace QAsist.Application.Interfaces.IRepositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<RefreshToken?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<bool> RevokeAsync(string token, CancellationToken cancellationToken = default);
        Task<bool> RevokeBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<bool> RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteExpiredAsync(CancellationToken cancellationToken = default);
    }
}
