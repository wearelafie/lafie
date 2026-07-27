using System.Security.Claims;

namespace Lafie.Core.Context;

/// <summary>Contexte de l'utilisateur authentifié courant.</summary>
public interface ICurrentUser
{
    string? Name { get; }
    Guid GetUserId();
    string? GetUserEmail();
    string? GetTenant();
    bool IsAuthenticated();
    bool IsInRole(string role);
    IEnumerable<Claim>? GetUserClaims();
}
