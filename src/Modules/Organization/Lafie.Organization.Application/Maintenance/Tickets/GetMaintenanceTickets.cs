using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Liste paginée et filtrée des tickets d'un établissement.</summary>
public sealed record GetMaintenanceTicketsQuery(Guid FacilityId, MaintenanceTicketFilter Filter)
    : IRequest<Result<PagedResult<MaintenanceTicketDto>>>;

public sealed class GetMaintenanceTicketsHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<GetMaintenanceTicketsQuery, Result<PagedResult<MaintenanceTicketDto>>>
{
    public async ValueTask<Result<PagedResult<MaintenanceTicketDto>>> Handle(GetMaintenanceTicketsQuery query, CancellationToken cancellationToken)
    {
        var page = await tickets.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
