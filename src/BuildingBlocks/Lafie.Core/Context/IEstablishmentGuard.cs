namespace Lafie.Core.Context;

/// <summary>Vérifie que l'utilisateur agit bien dans son établissement actif (ABAC).</summary>
public interface IEstablishmentGuard
{
    /// <summary>Autorise si super-admin (permission "*") ou si l'établissement actif correspond ;
    /// lève une ForbiddenException sinon. Le facilityId doit être dérivé côté serveur, jamais du client.</summary>
    void EnsureActiveFacility(Guid facilityId);
}
