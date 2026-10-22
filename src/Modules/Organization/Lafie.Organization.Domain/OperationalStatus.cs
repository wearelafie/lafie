namespace Lafie.Organization.Domain;

/// <summary>État opérationnel d'un établissement (distinct du statut administratif et de la configuration).</summary>
public enum OperationalStatus
{
    /// <summary>En cours d'installation (pas encore ouvert).</summary>
    Setup = 1,
    Active = 2,
    TemporarilyClosed = 3,
    Suspended = 4,
    PermanentlyClosed = 5,
}
