namespace Lafie.Organization.Application.References;

/// <summary>Accès aux données de référence (listes de codes + divisions géographiques).</summary>
public interface IReferenceDataRepository
{
    // --- Lecture (formulaires) ---
    Task<IReadOnlyList<ReferenceValueDto>> GetReferenceValuesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>Vrai si le code existe (et est actif) dans la catégorie donnée.</summary>
    Task<bool> ReferenceValueExistsAsync(string category, string code, CancellationToken cancellationToken = default);

    /// <summary>Divisions d'un niveau donné, filtrées par parent (code) si fourni.</summary>
    Task<IReadOnlyList<GeoDivisionDto>> GetGeoDivisionsAsync(string level, string? parentCode, CancellationToken cancellationToken = default);

    // --- Administration : valeurs de référence ---
    Task<IReadOnlyList<ReferenceCategoryDto>> GetReferenceCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceValueAdminDto>> GetReferenceValuesAdminAsync(string category, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    /// <summary>Vrai si le code existe, quel que soit son état actif.</summary>
    Task<bool> ReferenceValueCodeTakenAsync(string category, string code, CancellationToken cancellationToken = default);
    Task CreateReferenceValueAsync(string category, string code, string labelFr, string labelEn, int displayOrder, bool active, CancellationToken cancellationToken = default);
    Task<bool> UpdateReferenceValueAsync(string category, string code, string labelFr, string labelEn, int displayOrder, bool active, CancellationToken cancellationToken = default);
    Task<bool> SetReferenceValueActiveAsync(string category, string code, bool active, CancellationToken cancellationToken = default);
    Task<bool> DeleteReferenceValueAsync(string category, string code, CancellationToken cancellationToken = default);

    // --- Administration : divisions géographiques ---
    Task<bool> GeoDivisionCodeTakenAsync(string code, CancellationToken cancellationToken = default);
    Task<Guid?> GetGeoDivisionIdByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Guid> CreateGeoDivisionAsync(string level, string code, string name, Guid? parentId, int displayOrder, CancellationToken cancellationToken = default);
    Task<bool> UpdateGeoDivisionAsync(Guid id, string name, Guid? parentId, int displayOrder, CancellationToken cancellationToken = default);
    Task<int> CountGeoChildrenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteGeoDivisionAsync(Guid id, CancellationToken cancellationToken = default);
}
