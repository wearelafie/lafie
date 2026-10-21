namespace Lafie.Organization.Domain;

/// <summary>État opérationnel d'un espace (y compris indisponibilité temporaire).</summary>
public enum SpaceStatus
{
    Active = 1,
    Inactive = 2,
    Maintenance = 3,
}
