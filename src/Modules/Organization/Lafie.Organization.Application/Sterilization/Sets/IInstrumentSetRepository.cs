using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Critères de recherche des boîtes d'instruments d'un établissement.</summary>
public sealed record InstrumentSetFilter(
    string? Search = null, string? Status = null, string? Category = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des boîtes d'instruments et de leurs affectations (CSSD).</summary>
public interface IInstrumentSetRepository
{
    Task AddAsync(InstrumentSet set, CancellationToken cancellationToken = default);
    Task UpdateAsync(InstrumentSet set, CancellationToken cancellationToken = default);
    Task<InstrumentSet?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentSetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentSetDto>> SearchByFacilityAsync(Guid facilityId, InstrumentSetFilter filter, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId, CancellationToken cancellationToken = default);

    // Affectations (lien avec les interventions).
    Task AddUsageAsync(InstrumentSetUsage usage, CancellationToken cancellationToken = default);
    Task UpdateUsageAsync(InstrumentSetUsage usage, CancellationToken cancellationToken = default);
    Task<InstrumentSetUsage?> FindUsageAsync(Guid usageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstrumentSetUsageDto>> GetUsagesAsync(Guid setId, CancellationToken cancellationToken = default);
}
