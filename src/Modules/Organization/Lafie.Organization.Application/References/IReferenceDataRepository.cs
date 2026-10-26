namespace Lafie.Organization.Application.References;

/// <summary>Accès en lecture aux données de référence (listes de codes + divisions géographiques).</summary>
public interface IReferenceDataRepository
{
    Task<IReadOnlyList<ReferenceValueDto>> GetReferenceValuesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>Vrai si le code existe (et est actif) dans la catégorie donnée.</summary>
    Task<bool> ReferenceValueExistsAsync(string category, string code, CancellationToken cancellationToken = default);

    /// <summary>Divisions d'un niveau donné, filtrées par parent (code) si fourni.</summary>
    Task<IReadOnlyList<GeoDivisionDto>> GetGeoDivisionsAsync(string level, string? parentCode, CancellationToken cancellationToken = default);
}
