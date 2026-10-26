namespace Lafie.Organization.Application.References;

/// <summary>Division administrative (pays / région / préfecture / commune), mappée par Dapper.</summary>
public sealed class GeoDivisionDto
{
    public Guid Id { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ParentCode { get; init; }
    public int DisplayOrder { get; init; }
}
