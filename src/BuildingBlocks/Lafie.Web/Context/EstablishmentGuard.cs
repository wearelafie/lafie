using Lafie.Core.Context;
using Lafie.Core.Exceptions;

namespace Lafie.Web.Context;

/// <summary>Garde ABAC : l'utilisateur ne peut agir que dans son établissement actif (ou s'il est super-admin).</summary>
public sealed class EstablishmentGuard(ICurrentUser currentUser) : IEstablishmentGuard
{
    public void EnsureActiveFacility(Guid facilityId)
    {
        var claims = currentUser.GetUserClaims();
        if (claims is not null && claims.Any(c => c.Type == "perm" && c.Value == "*"))
            return; // super-admin : accès transverse

        if (Guid.TryParse(currentUser.GetTenant(), out var active) && active == facilityId)
            return;

        throw new ForbiddenException("Action hors de votre établissement actif.");
    }
}
