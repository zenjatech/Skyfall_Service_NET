using Microsoft.Azure.Functions.Worker.Http;
using Skyfall.Domain.Entities;
using System.Security.Claims;

namespace Skyfall.Common;

public static class AuthHelper
{
    public static (ClaimsPrincipal? principal, Guid tenantId, Task<HttpResponseData>? error)
        Authorize(HttpRequestData req, JwtHelper jwt, string? requiredRole = null)
    {
        var authHeader = req.Headers.FirstOrDefault(h =>
            string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            return (null, Guid.Empty, ResponseFactory.UnauthorizedAsync(req));

        var principal = jwt.ValidateToken(authHeader["Bearer ".Length..]);
        if (principal is null) return (null, Guid.Empty, ResponseFactory.UnauthorizedAsync(req));

        var tenantId = JwtHelper.GetTenantId(principal);
        if (!tenantId.HasValue) return (null, Guid.Empty, ResponseFactory.UnauthorizedAsync(req));

        if (requiredRole is not null)
        {
            var role = JwtHelper.GetRole(principal);
            if (role != requiredRole && role != StaffRoles.Admin)
                return (null, Guid.Empty, ResponseFactory.ForbiddenAsync(req));
        }

        return (principal, tenantId.Value, null);
    }
}
