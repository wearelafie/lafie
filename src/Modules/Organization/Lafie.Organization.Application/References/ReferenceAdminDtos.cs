namespace Lafie.Organization.Application.References;

/// <summary>Valeur de référence pour l'administration (inclut l'état actif).</summary>
public sealed class ReferenceValueAdminDto
{
    public string Category { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string LabelFr { get; init; } = string.Empty;
    public string LabelEn { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool Active { get; init; }
}

/// <summary>Catégorie de référentiel + nombre de codes (pour le sélecteur d'administration).</summary>
public sealed class ReferenceCategoryDto
{
    public string Category { get; init; } = string.Empty;
    public int Count { get; init; }
}
