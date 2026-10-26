using Lafie.Core.Common;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.CareUnits;

/// <summary>Critères de recherche/pagination des unités de soins (au sein d'un service).</summary>
public sealed record CareUnitFilter(string? Search = null, string? Type = null, string? Status = null, bool IncludeArchived = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des unités de soins (et de leur association aux espaces).</summary>
public interface ICareUnitRepository
{
    Task AddAsync(CareUnit careUnit, CancellationToken cancellationToken = default);
    Task UpdateAsync(CareUnit careUnit, CancellationToken cancellationToken = default);
    Task<CareUnit?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CareUnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<CareUnitDto>> SearchByServiceAsync(Guid serviceId, CareUnitFilter filter, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // --- Association aux espaces (M:N) ---
    Task<bool> SpaceLinkExistsAsync(Guid careUnitId, Guid spaceId, CancellationToken cancellationToken = default);
    Task AddSpaceAsync(Guid careUnitId, Guid spaceId, CancellationToken cancellationToken = default);
    Task RemoveSpaceAsync(Guid careUnitId, Guid spaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpaceDto>> GetSpacesAsync(Guid careUnitId, CancellationToken cancellationToken = default);
}
