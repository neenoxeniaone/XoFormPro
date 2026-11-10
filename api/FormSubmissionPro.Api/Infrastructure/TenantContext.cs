using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FormSubmissionPro.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FormSubmissionPro.Api.Infrastructure;

/// <summary>
/// Resolves the current tenant + user from the authenticated JWT claims.
/// </summary>
public static class TenantContext
{
    public static bool TryGetOrgId(this ControllerBase controller, out Guid orgId)
    {
        orgId = Guid.Empty;
        var value = controller.User.FindFirst(JwtTokenService.OrgClaim)?.Value;
        return Guid.TryParse(value, out orgId);
    }

    /// <summary>The current user's id from the JWT subject claim (sub / nameidentifier).</summary>
    public static Guid? GetUserId(this ControllerBase controller)
    {
        var value = controller.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
