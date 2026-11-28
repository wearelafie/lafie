using Lafie.Core.Common;
using Lafie.Pharmacy.Domain;

namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Critères de recherche du catalogue médicaments d'un établissement.</summary>
public sealed record MedicationFilter(
    string? Search = null, string? Form = null, string? Status = null, bool? Controlled = null,
    int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des fiches médicament (catalogue, schéma `pharmacy`).</summary>
public interface IMedicationRepository
{
    Task AddAsync(Medication medication, CancellationToken cancellationToken = default);
    Task UpdateAsync(Medication medication, CancellationToken cancellationToken = default);
    Task<Medication?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<PagedResult<MedicationDto>> SearchByFacilityAsync(Guid facilityId, MedicationFilter filter, CancellationToken cancellationToken = default);
}
