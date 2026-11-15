using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Waste.Collections;

/// <summary>Critères de recherche des collectes de déchets d'un établissement.</summary>
public sealed record WasteCollectionFilter(
    string? Search = null, string? Stream = null, string? Status = null, bool UnassignedOnly = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des collectes pesées (DASRI &amp; assimilés).</summary>
public interface IWasteCollectionRepository
{
    Task AddAsync(WasteCollection collection, CancellationToken cancellationToken = default);
    Task UpdateAsync(WasteCollection collection, CancellationToken cancellationToken = default);
    Task<WasteCollection?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WasteCollectionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WasteCollectionDto>> SearchByFacilityAsync(Guid facilityId, WasteCollectionFilter filter, CancellationToken cancellationToken = default);

    // Lignes d'un bordereau.
    Task<IReadOnlyList<WasteCollectionDto>> GetBySlipAsync(Guid slipId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSlipCollectionIdsAsync(Guid slipId, CancellationToken cancellationToken = default);
    Task<decimal> SumWeightForSlipAsync(Guid slipId, CancellationToken cancellationToken = default);
}
