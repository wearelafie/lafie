using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Ajoute une pièce détachée consommée sur un ticket.</summary>
public sealed record AddTicketPartCommand(
    Guid TicketId, string Label, string? Reference, int Quantity, decimal? UnitCost) : IRequest<Result<Guid>>;

public sealed class AddTicketPartHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<AddTicketPartCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(AddTicketPartCommand command, CancellationToken cancellationToken)
    {
        var ticket = await tickets.FindAsync(command.TicketId, cancellationToken)
            ?? throw new NotFoundException($"Ticket {command.TicketId} introuvable.");

        var part = MaintenanceTicketPart.Create(
            ticket.Id, command.Label, command.Reference, command.Quantity, command.UnitCost);
        if (part.IsFailure) return Result.Failure<Guid>(part.Error!);

        await tickets.AddPartAsync(part.Value, cancellationToken);
        return Result.Success(part.Value.Id);
    }
}
