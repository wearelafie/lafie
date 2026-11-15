namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Projection lecture d'un ticket de maintenance (Dapper, par nom de colonne). Champs cible enrichis par jointure.</summary>
public sealed class MaintenanceTicketDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string? TargetName { get; init; }
    public string? TargetCode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Criticality { get; init; } = string.Empty;
    public bool Blocking { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AssigneeType { get; init; }
    public string? AssigneeRef { get; init; }
    public string? ReportedBy { get; init; }
    public string? Resolution { get; init; }
    public DateTime? DowntimeStart { get; init; }
    public DateTime? DowntimeEnd { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

/// <summary>Pièce détachée consommée sur un ticket.</summary>
public sealed class MaintenanceTicketPartDto
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public int Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
