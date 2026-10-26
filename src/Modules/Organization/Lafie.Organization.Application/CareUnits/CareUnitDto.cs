namespace Lafie.Organization.Application.CareUnits;

/// <summary>Projection lecture d'une unité de soins (mappée par Dapper, par nom de colonne).</summary>
public sealed class CareUnitDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid ServiceId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int? BedCapacity { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? ArchivedOnUtc { get; init; }
}
