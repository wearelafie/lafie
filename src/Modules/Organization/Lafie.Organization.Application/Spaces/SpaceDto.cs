namespace Lafie.Organization.Application.Spaces;

/// <summary>Projection lecture d'un espace (mappée par Dapper, par nom de colonne).</summary>
public sealed class SpaceDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid BuildingId { get; init; }
    public Guid FloorId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? Area { get; init; }
    public int? MaxCapacity { get; init; }
    public bool Accessible { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
