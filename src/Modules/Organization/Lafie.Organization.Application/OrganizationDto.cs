namespace Lafie.Organization.Application;

/// <summary>Projection lecture d'une organisation (côté API). Mappée par Dapper (par nom de colonne).</summary>
public sealed class OrganizationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Code { get; init; }
    public bool Active { get; init; }
    public Guid? ParentId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
