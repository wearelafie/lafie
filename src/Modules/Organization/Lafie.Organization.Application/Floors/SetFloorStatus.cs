using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Floors;

/// <summary>Change l'état opérationnel d'un étage.</summary>
public sealed record SetFloorStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetFloorStatusHandler(IFloorRepository floors) : IRequestHandler<SetFloorStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetFloorStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FloorStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"Statut inconnu : '{command.Status}'.");

        var floor = await floors.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Étage {command.Id} introuvable.");

        floor.SetStatus(status);
        await floors.UpdateAsync(floor, cancellationToken);
        return Result.Success();
    }
}
