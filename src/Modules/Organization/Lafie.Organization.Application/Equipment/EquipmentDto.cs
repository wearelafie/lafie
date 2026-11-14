namespace Lafie.Organization.Application.Equipment;

/// <summary>Projection lecture d'un équipement (mappée par Dapper, par nom de colonne).</summary>
public sealed class EquipmentDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string InventoryNumber { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public DateOnly? AcquisitionDate { get; init; }
    public string? Supplier { get; init; }
    public DateOnly? WarrantyEnd { get; init; }
    public int? EstimatedLifespanMonths { get; init; }
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public Guid? ServiceId { get; init; }
    public string? ServiceName { get; init; }
    public Guid? CareUnitId { get; init; }
    public string? CareUnitName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? ArchivedOnUtc { get; init; }
}
