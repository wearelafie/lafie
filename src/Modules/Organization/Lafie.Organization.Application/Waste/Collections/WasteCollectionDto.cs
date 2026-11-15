namespace Lafie.Organization.Application.Waste.Collections;

/// <summary>Projection lecture d'une collecte pesée (Dapper, par nom de colonne).</summary>
public sealed class WasteCollectionDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Stream { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public int ContainerCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? SpaceId { get; init; }
    public string? SpaceName { get; init; }
    public string? SpaceCode { get; init; }
    public Guid? SlipId { get; init; }
    public string? SlipReference { get; init; }
    public DateOnly? CollectedOn { get; init; }
    public string? CollectedBy { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
