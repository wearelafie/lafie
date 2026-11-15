using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Détail d'un ticket avec ses pièces détachées.</summary>
public sealed record MaintenanceTicketDetailDto(MaintenanceTicketDto Ticket, IReadOnlyList<MaintenanceTicketPartDto> Parts);

public sealed record GetMaintenanceTicketQuery(Guid Id) : IRequest<Result<MaintenanceTicketDetailDto>>;

public sealed class GetMaintenanceTicketHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<GetMaintenanceTicketQuery, Result<MaintenanceTicketDetailDto>>
{
    public async ValueTask<Result<MaintenanceTicketDetailDto>> Handle(GetMaintenanceTicketQuery query, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetByIdAsync(query.Id, cancellationToken);
        if (ticket is null) return Result.Failure<MaintenanceTicketDetailDto>("Ticket introuvable.");

        var parts = await tickets.GetPartsAsync(query.Id, cancellationToken);
        return Result.Success(new MaintenanceTicketDetailDto(ticket, parts));
    }
}
