namespace Lafie.Organization.Application;

/// <summary>Projection lecture d'une organisation (côté API). Mappée par Dapper (par nom de colonne).</summary>
public sealed class OrganizationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public string? Description { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string Timezone { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Country { get; init; }
    public Guid? ParentId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
