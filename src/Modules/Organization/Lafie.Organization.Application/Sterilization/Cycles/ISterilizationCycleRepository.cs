using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Critères de recherche des cycles d'autoclave d'un établissement.</summary>
public sealed record SterilizationCycleFilter(
    string? Search = null, string? Status = null, bool OpenOnly = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des cycles d'autoclave et de leur charge (CSSD).</summary>
public interface ISterilizationCycleRepository
{
    Task AddAsync(SterilizationCycle cycle, CancellationToken cancellationToken = default);
    Task UpdateAsync(SterilizationCycle cycle, CancellationToken cancellationToken = default);
    Task<SterilizationCycle?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SterilizationCycleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<SterilizationCycleDto>> SearchByFacilityAsync(Guid facilityId, SterilizationCycleFilter filter, CancellationToken cancellationToken = default);

    // Charge (boîtes du cycle).
    Task AddToLoadAsync(Guid cycleId, Guid setId, CancellationToken cancellationToken = default);
    Task RemoveFromLoadAsync(Guid cycleId, Guid setId, CancellationToken cancellationToken = default);
    Task<bool> IsInLoadAsync(Guid cycleId, Guid setId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetLoadSetIdsAsync(Guid cycleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CycleSetDto>> GetLoadAsync(Guid cycleId, CancellationToken cancellationToken = default);
}
