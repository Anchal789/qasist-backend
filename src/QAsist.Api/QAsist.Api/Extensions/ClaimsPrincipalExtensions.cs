using System.Security.Claims;

namespace QAsist.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("User ID not found in claims");

            return userId;
        }

        public static string GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value
                ?? throw new UnauthorizedAccessException("Email not found in claims");
        }

        public static string GetSessionId(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("SessionId")?.Value
                ?? throw new UnauthorizedAccessException("Session ID not found in claims");
        }

        public static string GetRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Role)?.Value
                ?? throw new UnauthorizedAccessException("Role not found in claims");
        }
    }
}
