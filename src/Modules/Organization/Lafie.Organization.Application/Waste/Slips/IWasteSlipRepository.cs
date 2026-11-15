using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Critères de recherche des bordereaux d'élimination d'un établissement.</summary>
public sealed record WasteSlipFilter(
    string? Search = null, string? Stream = null, string? Status = null, bool OpenOnly = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des bordereaux d'élimination (DASRI &amp; assimilés).</summary>
public interface IWasteSlipRepository
{
    Task AddAsync(WasteEliminationSlip slip, CancellationToken cancellationToken = default);
    Task UpdateAsync(WasteEliminationSlip slip, CancellationToken cancellationToken = default);
    Task<WasteEliminationSlip?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WasteSlipDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WasteSlipDto>> SearchByFacilityAsync(Guid facilityId, WasteSlipFilter filter, CancellationToken cancellationToken = default);
    Task<bool> ReferenceExistsAsync(Guid facilityId, string reference, Guid? excludeId, CancellationToken cancellationToken = default);
}
