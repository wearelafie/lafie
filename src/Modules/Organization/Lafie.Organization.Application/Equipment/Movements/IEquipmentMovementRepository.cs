using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Equipment.Movements;

/// <summary>Critères de pagination des déplacements d'un équipement.</summary>
public sealed record EquipmentMovementFilter(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des déplacements d'équipement.</summary>
public interface IEquipmentMovementRepository
{
    Task AddAsync(EquipmentMovement movement, CancellationToken cancellationToken = default);
    Task<EquipmentMovementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<EquipmentMovementDto>> SearchByEquipmentAsync(Guid equipmentId, EquipmentMovementFilter filter, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
