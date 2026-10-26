namespace Lafie.Organization.Application.Services;

/// <summary>Projection lecture d'un service (mappée par Dapper, par nom de colonne).</summary>
public sealed class ServiceDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid? ParentServiceId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Head { get; init; }
    public int DisplayOrder { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? ArchivedOnUtc { get; init; }
}
