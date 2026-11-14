namespace Lafie.Organization.Application.Equipment.Maintenance;

/// <summary>Projection lecture d'une intervention de maintenance.</summary>
public sealed class EquipmentMaintenanceDto
{
    public Guid Id { get; init; }
    public Guid EquipmentId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly? ScheduledOn { get; init; }
    public DateOnly? PerformedOn { get; init; }
    public string? Provider { get; init; }
    public decimal? Cost { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
