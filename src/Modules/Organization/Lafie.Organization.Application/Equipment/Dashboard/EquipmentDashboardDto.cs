namespace Lafie.Organization.Application.Equipment.Dashboard;

/// <summary>Décompte d'une catégorie d'équipement (code + effectif).</summary>
public sealed record EquipmentCategoryCount(string Category, int Count);

/// <summary>Alerte de garantie : équipement dont la garantie est expirée ou proche de l'échéance.</summary>
public sealed record WarrantyAlertDto(
    Guid EquipmentId, string InventoryNumber, string Name, string Category, string Status,
    DateOnly WarrantyEnd, int DaysRemaining);

/// <summary>Alerte de maintenance : intervention planifiée en retard ou à venir.</summary>
public sealed record MaintenanceAlertDto(
    Guid MaintenanceId, Guid EquipmentId, string InventoryNumber, string EquipmentName,
    string Type, string Status, DateOnly ScheduledOn, int DaysRemaining, string Description);

/// <summary>Synthèse du parc d'équipements d'un établissement + alertes garantie/maintenance.</summary>
public sealed class EquipmentDashboardDto
{
    public int Total { get; init; }
    public int Operational { get; init; }
    public int UnderMaintenance { get; init; }
    public int OutOfService { get; init; }
    public int Decommissioned { get; init; }

    public IReadOnlyList<EquipmentCategoryCount> ByCategory { get; init; } = [];

    /// <summary>Garanties déjà expirées (équipements non réformés).</summary>
    public IReadOnlyList<WarrantyAlertDto> WarrantyExpired { get; init; } = [];
    /// <summary>Garanties expirant dans la fenêtre d'alerte.</summary>
    public IReadOnlyList<WarrantyAlertDto> WarrantyExpiring { get; init; } = [];

    /// <summary>Interventions planifiées/en cours dont la date prévue est dépassée.</summary>
    public IReadOnlyList<MaintenanceAlertDto> MaintenanceOverdue { get; init; } = [];
    /// <summary>Interventions planifiées dans la fenêtre d'alerte.</summary>
    public IReadOnlyList<MaintenanceAlertDto> MaintenanceUpcoming { get; init; } = [];
}
