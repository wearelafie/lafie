using System.Security.Claims;
using Lafie.Core.Context;
using Microsoft.AspNetCore.Http;

namespace Lafie.Web.Context;

/// <summary>Contexte utilisateur courant dérivé des claims JWT de la requête HTTP.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public string? Name => User?.FindFirst("name")?.Value ?? User?.Identity?.Name;

    public Guid GetUserId()
    {
        var id = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }

    public string? GetUserEmail() => User?.FindFirst(ClaimTypes.Email)?.Value ?? User?.FindFirst("email")?.Value;

    public string? GetTenant() => User?.FindFirst("tenant")?.Value;

    public bool IsAuthenticated() => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public IEnumerable<Claim>? GetUserClaims() => User?.Claims;
}
