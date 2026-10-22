namespace Lafie.Organization.Application.Buildings;

/// <summary>Projection lecture d'un bâtiment (mappée par Dapper, par nom de colonne).</summary>
public sealed class BuildingDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? ArchivedOnUtc { get; init; }
}
