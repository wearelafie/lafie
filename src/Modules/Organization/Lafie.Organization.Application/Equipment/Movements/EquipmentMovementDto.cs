namespace Lafie.Organization.Application.Equipment.Movements;

/// <summary>Projection lecture d'un déplacement d'équipement (jointures espaces).</summary>
public sealed class EquipmentMovementDto
{
    public Guid Id { get; init; }
    public Guid EquipmentId { get; init; }
    public DateOnly MovedOn { get; init; }
    public Guid? FromSpaceId { get; init; }
    public string? FromSpaceName { get; init; }
    public Guid? ToSpaceId { get; init; }
    public string? ToSpaceName { get; init; }
    public string? Reason { get; init; }
    public string? PerformedBy { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
