using QAsist.Domain.Entities;
using System.Security.Claims;

namespace QAsist.Application.Interfaces.IServices
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user, string sessionId);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        string? GetClaimValue(ClaimsPrincipal principal, string claimType);
    }
}
