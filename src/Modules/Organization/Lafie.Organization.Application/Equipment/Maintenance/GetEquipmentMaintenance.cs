using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Maintenance;

/// <summary>Liste paginée/filtrée des interventions de maintenance d'un équipement.</summary>
public sealed record GetEquipmentMaintenanceQuery(Guid EquipmentId, EquipmentMaintenanceFilter Filter)
    : IRequest<PagedResult<EquipmentMaintenanceDto>>;

public sealed class GetEquipmentMaintenanceHandler(IEquipmentMaintenanceRepository maintenances)
    : IRequestHandler<GetEquipmentMaintenanceQuery, PagedResult<EquipmentMaintenanceDto>>
{
    public async ValueTask<PagedResult<EquipmentMaintenanceDto>> Handle(GetEquipmentMaintenanceQuery query, CancellationToken cancellationToken)
        => await maintenances.SearchByEquipmentAsync(query.EquipmentId, query.Filter, cancellationToken);
}
