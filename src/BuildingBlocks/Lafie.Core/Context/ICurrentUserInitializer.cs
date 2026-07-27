using System.Security.Claims;

namespace Lafie.Core.Context;

/// <summary>Initialise le contexte utilisateur courant.</summary>
public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);
    void SetCurrentUserId(string userId);
}
