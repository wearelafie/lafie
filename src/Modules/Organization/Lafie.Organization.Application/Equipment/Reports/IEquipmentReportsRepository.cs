using Lafie.Core.Common;

namespace Lafie.Organization.Application.Equipment.Reports;

/// <summary>Lectures transverses « équipement » d'un établissement : garanties et maintenance (vues globales).</summary>
public interface IEquipmentReportsRepository
{
    Task<PagedResult<EquipmentWarrantyDto>> SearchWarrantiesAsync(Guid facilityId, EquipmentWarrantyFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResult<FacilityMaintenanceDto>> SearchMaintenanceAsync(Guid facilityId, FacilityMaintenanceFilter filter, CancellationToken cancellationToken = default);
}
