using Microsoft.AspNetCore.Authorization;
using QAsist.Domain.Enums;
using System.Data;

namespace QAsist.Api.Filters
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params UserRole[] roles)
        {
            Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }
}
