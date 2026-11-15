using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Affecte un ticket à un intervenant interne ou externe.</summary>
public sealed record AssignMaintenanceTicketCommand(Guid Id, string AssigneeType, string AssigneeRef) : IRequest<Result>;

public sealed class AssignMaintenanceTicketHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<AssignMaintenanceTicketCommand, Result>
{
    public async ValueTask<Result> Handle(AssignMaintenanceTicketCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TicketAssigneeType>(command.AssigneeType, ignoreCase: true, out var assigneeType))
            return Result.Failure($"Type d'intervenant inconnu : '{command.AssigneeType}'.");

        var ticket = await tickets.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Ticket {command.Id} introuvable.");

        var result = ticket.Assign(assigneeType, command.AssigneeRef);
        if (result.IsFailure) return result;

        await tickets.UpdateAsync(ticket, cancellationToken);
        return Result.Success();
    }
}
