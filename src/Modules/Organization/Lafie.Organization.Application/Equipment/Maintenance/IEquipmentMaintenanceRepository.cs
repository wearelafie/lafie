using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Equipment.Maintenance;

/// <summary>Critères de recherche/pagination des interventions d'un équipement.</summary>
public sealed record EquipmentMaintenanceFilter(string? Type = null, string? Status = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des interventions de maintenance.</summary>
public interface IEquipmentMaintenanceRepository
{
    Task AddAsync(EquipmentMaintenance maintenance, CancellationToken cancellationToken = default);
    Task UpdateAsync(EquipmentMaintenance maintenance, CancellationToken cancellationToken = default);
    Task<EquipmentMaintenance?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EquipmentMaintenanceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<EquipmentMaintenanceDto>> SearchByEquipmentAsync(Guid equipmentId, EquipmentMaintenanceFilter filter, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
