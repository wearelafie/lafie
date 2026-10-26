using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Spaces;

/// <summary>Critères de recherche/pagination des espaces (au sein d'un étage).</summary>
public sealed record SpaceFilter(string? Search = null, string? Type = null, string? Status = null, bool IncludeArchived = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des espaces.</summary>
public interface ISpaceRepository
{
    Task AddAsync(Space space, CancellationToken cancellationToken = default);
    Task UpdateAsync(Space space, CancellationToken cancellationToken = default);
    Task<Space?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SpaceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<SpaceDto>> SearchByFloorAsync(Guid floorId, SpaceFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResult<SpaceDto>> SearchByFacilityAsync(Guid facilityId, SpaceFilter filter, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
