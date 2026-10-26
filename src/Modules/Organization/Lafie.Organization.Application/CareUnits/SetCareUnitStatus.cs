using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.CareUnits;

/// <summary>Change l'état d'une unité de soins.</summary>
public sealed record SetCareUnitStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetCareUnitStatusHandler(ICareUnitRepository careUnits)
    : IRequestHandler<SetCareUnitStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetCareUnitStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CareUnitStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"Statut inconnu : '{command.Status}'.");

        var unit = await careUnits.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Unité de soins {command.Id} introuvable.");

        unit.SetStatus(status);
        await careUnits.UpdateAsync(unit, cancellationToken);
        return Result.Success();
    }
}
