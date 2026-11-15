using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Fait avancer un ticket : start | resolve | close. Déverrouille l'espace si plus aucun ticket bloquant actif.</summary>
public sealed record TransitionMaintenanceTicketCommand(Guid Id, string Action, string? Resolution = null) : IRequest<Result>;

public sealed class TransitionMaintenanceTicketHandler(
    IMaintenanceTicketRepository tickets,
    ISpaceRepository spaces) : IRequestHandler<TransitionMaintenanceTicketCommand, Result>
{
    public async ValueTask<Result> Handle(TransitionMaintenanceTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await tickets.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Ticket {command.Id} introuvable.");

        var result = command.Action.ToLowerInvariant() switch
        {
            "start" => ticket.Start(),
            "resolve" => ticket.Resolve(command.Resolution),
            "close" => ticket.Close(),
            _ => Result.Failure($"Action inconnue : '{command.Action}'."),
        };
        if (result.IsFailure) return result;

        await tickets.UpdateAsync(ticket, cancellationToken);

        // Déverrouillage : si le ticket ne bloque plus et visait un espace, libérer l'espace
        // dès lors qu'aucun autre ticket bloquant actif ne subsiste.
        if (ticket.Blocking && ticket.TargetType is TicketTargetType.Space && !ticket.IsActivelyBlocking)
        {
            var remaining = await tickets.CountActiveBlockingForSpaceAsync(ticket.TargetId, ticket.Id, cancellationToken);
            if (remaining == 0)
            {
                var space = await spaces.FindAsync(ticket.TargetId, cancellationToken);
                if (space is not null && space.OperationalState == SpaceOperationalState.Blocked)
                {
                    space.SetOperationalState(SpaceOperationalState.Available);
                    await spaces.UpdateAsync(space, cancellationToken);
                }
            }
        }

        return Result.Success();
    }
}
