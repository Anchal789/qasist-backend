using QAsist.Application.DTOs;

namespace QAsist.Application.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
        Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
        Task LogoutAsync(string sessionId, CancellationToken cancellationToken = default);
        Task LogoutAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
