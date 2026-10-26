namespace Lafie.Organization.Application.References;

/// <summary>Valeur de référence (liste de codes configurable), mappée par Dapper.</summary>
public sealed class ReferenceValueDto
{
    public string Category { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string LabelFr { get; init; } = string.Empty;
    public string LabelEn { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
}
