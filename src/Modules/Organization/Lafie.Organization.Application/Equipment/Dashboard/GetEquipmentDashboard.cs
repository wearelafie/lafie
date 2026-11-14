using Mediator;

namespace Lafie.Organization.Application.Equipment.Dashboard;

/// <summary>Tableau de bord des équipements d'un établissement (synthèse + alertes).</summary>
public sealed record GetEquipmentDashboardQuery(Guid FacilityId, EquipmentDashboardOptions Options)
    : IRequest<EquipmentDashboardDto>;

public sealed class GetEquipmentDashboardHandler(IEquipmentDashboardRepository dashboard)
    : IRequestHandler<GetEquipmentDashboardQuery, EquipmentDashboardDto>
{
    public async ValueTask<EquipmentDashboardDto> Handle(GetEquipmentDashboardQuery query, CancellationToken cancellationToken)
        => await dashboard.GetAsync(query.FacilityId, query.Options, cancellationToken);
}
