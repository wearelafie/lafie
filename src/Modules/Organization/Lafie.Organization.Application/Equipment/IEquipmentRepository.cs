using Lafie.Core.Common;
using DomainEquipment = Lafie.Organization.Domain.Equipment;

namespace Lafie.Organization.Application.Equipment;

/// <summary>Critères de recherche/pagination des équipements (au sein d'un établissement).</summary>
public sealed record EquipmentFilter(
    string? Search = null, string? Category = null, string? Status = null,
    Guid? SpaceId = null, Guid? ServiceId = null, bool IncludeArchived = false,
    int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des équipements.</summary>
public interface IEquipmentRepository
{
    Task AddAsync(DomainEquipment equipment, CancellationToken cancellationToken = default);
    Task UpdateAsync(DomainEquipment equipment, CancellationToken cancellationToken = default);
    Task<DomainEquipment?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EquipmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<EquipmentDto>> SearchAsync(Guid facilityId, EquipmentFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Vrai si un numéro d'inventaire existe déjà dans l'établissement (hors équipement <paramref name="excludeId"/>).</summary>
    Task<bool> InventoryNumberExistsAsync(Guid facilityId, string inventoryNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
