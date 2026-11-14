namespace Lafie.Organization.Domain;

/// <summary>Statut d'une intervention de maintenance.</summary>
public enum MaintenanceStatus
{
    /// <summary>Planifiée (à venir).</summary>
    Planned = 1,
    /// <summary>En cours.</summary>
    InProgress = 2,
    /// <summary>Terminée.</summary>
    Completed = 3,
    /// <summary>Annulée.</summary>
    Cancelled = 4,
}
