using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Modifie les champs d'un ticket (titre, description, criticité, caractère bloquant) et resynchronise le verrou espace.</summary>
public sealed record UpdateMaintenanceTicketCommand(
    Guid Id, string Title, string? Description, string Criticality, bool Blocking) : IRequest<Result>;

public sealed class UpdateMaintenanceTicketHandler(
    IMaintenanceTicketRepository tickets,
    ISpaceRepository spaces) : IRequestHandler<UpdateMaintenanceTicketCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateMaintenanceTicketCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TicketCriticality>(command.Criticality, ignoreCase: true, out var criticality))
            return Result.Failure($"Criticité inconnue : '{command.Criticality}'.");

        var ticket = await tickets.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Ticket {command.Id} introuvable.");

        var result = ticket.Edit(command.Title, command.Description, criticality, command.Blocking);
        if (result.IsFailure) return result;

        await tickets.UpdateAsync(ticket, cancellationToken);

        if (ticket.TargetType is TicketTargetType.Space)
        {
            var space = await spaces.FindAsync(ticket.TargetId, cancellationToken);
            if (space is not null)
            {
                if (ticket.IsActivelyBlocking && space.OperationalState != SpaceOperationalState.Blocked)
                {
                    space.SetOperationalState(SpaceOperationalState.Blocked);
                    await spaces.UpdateAsync(space, cancellationToken);
                }
                else if (!ticket.IsActivelyBlocking && space.OperationalState == SpaceOperationalState.Blocked
                    && await tickets.CountActiveBlockingForSpaceAsync(ticket.TargetId, ticket.Id, cancellationToken) == 0)
                {
                    space.SetOperationalState(SpaceOperationalState.Available);
                    await spaces.UpdateAsync(space, cancellationToken);
                }
            }
        }

        return Result.Success();
    }
}
