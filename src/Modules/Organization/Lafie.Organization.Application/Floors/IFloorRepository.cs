using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Floors;

/// <summary>Critères de recherche/pagination des étages (au sein d'un bâtiment).</summary>
public sealed record FloorFilter(string? Search = null, string? Status = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des étages.</summary>
public interface IFloorRepository
{
    Task AddAsync(Floor floor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Floor floor, CancellationToken cancellationToken = default);
    Task<Floor?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FloorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<FloorDto>> SearchAsync(Guid buildingId, FloorFilter filter, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid buildingId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
