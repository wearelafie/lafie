namespace Lafie.Organization.Application.Floors;

/// <summary>Projection lecture d'un étage (mappée par Dapper, par nom de colonne).</summary>
public sealed class FloorDto
{
    public Guid Id { get; init; }
    public Guid BuildingId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? Level { get; init; }
    public int DisplayOrder { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
