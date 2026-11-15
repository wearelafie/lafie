using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Retire une pièce détachée d'un ticket.</summary>
public sealed record DeleteTicketPartCommand(Guid PartId) : IRequest<Result>;

public sealed class DeleteTicketPartHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<DeleteTicketPartCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteTicketPartCommand command, CancellationToken cancellationToken)
    {
        await tickets.DeletePartAsync(command.PartId, cancellationToken);
        return Result.Success();
    }
}
