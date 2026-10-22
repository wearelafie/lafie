using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Buildings;

/// <summary>Critères de recherche/pagination des bâtiments (au sein d'un établissement).</summary>
public sealed record BuildingFilter(string? Search = null, string? Status = null, bool IncludeArchived = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des bâtiments.</summary>
public interface IBuildingRepository
{
    Task AddAsync(Building building, CancellationToken cancellationToken = default);
    Task UpdateAsync(Building building, CancellationToken cancellationToken = default);
    Task<Building?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<BuildingDto>> SearchAsync(Guid facilityId, BuildingFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Vrai si un code existe déjà dans l'établissement (hors bâtiment <paramref name="excludeId"/>).</summary>
    Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
