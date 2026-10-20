namespace Lafie.Organization.Domain;

/// <summary>Cycle de vie d'une organisation.</summary>
public enum OrganizationStatus
{
    /// <summary>En cours de configuration (pas encore exploitable).</summary>
    Draft = 1,

    /// <summary>Active / exploitable.</summary>
    Active = 2,

    /// <summary>Désactivée (désactivation logique).</summary>
    Inactive = 3,
}
