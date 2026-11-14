using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Reports;

/// <summary>Vue globale paginée/filtrée des interventions de maintenance d'un établissement.</summary>
public sealed record GetFacilityMaintenanceQuery(Guid FacilityId, FacilityMaintenanceFilter Filter)
    : IRequest<PagedResult<FacilityMaintenanceDto>>;

public sealed class GetFacilityMaintenanceHandler(IEquipmentReportsRepository reports)
    : IRequestHandler<GetFacilityMaintenanceQuery, PagedResult<FacilityMaintenanceDto>>
{
    public async ValueTask<PagedResult<FacilityMaintenanceDto>> Handle(GetFacilityMaintenanceQuery query, CancellationToken cancellationToken)
        => await reports.SearchMaintenanceAsync(query.FacilityId, query.Filter, cancellationToken);
}
