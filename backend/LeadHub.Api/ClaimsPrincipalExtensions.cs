using System.Security.Claims;

namespace LeadHub.Api;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The OIDC subject of the caller, used as the owner id of their forms. JwtBearer maps "sub" to
    /// ClaimTypes.NameIdentifier by default; the raw claim is checked too in case mapping is disabled.
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}
