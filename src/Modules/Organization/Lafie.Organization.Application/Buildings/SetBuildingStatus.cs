using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Buildings;

/// <summary>Change l'état opérationnel d'un bâtiment.</summary>
public sealed record SetBuildingStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetBuildingStatusHandler(IBuildingRepository buildings)
    : IRequestHandler<SetBuildingStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetBuildingStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BuildingStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"Statut inconnu : '{command.Status}'.");

        var building = await buildings.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bâtiment {command.Id} introuvable.");

        building.SetStatus(status);
        await buildings.UpdateAsync(building, cancellationToken);
        return Result.Success();
    }
}
