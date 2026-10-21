using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Floors;

/// <summary>Met à jour un étage.</summary>
public sealed record UpdateFloorCommand(
    Guid Id, string Code, string Name, int? Level = null, int DisplayOrder = 0, string? Description = null)
    : IRequest<Result>;

public sealed class UpdateFloorHandler(IFloorRepository floors) : IRequestHandler<UpdateFloorCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateFloorCommand command, CancellationToken cancellationToken)
    {
        var floor = await floors.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Étage {command.Id} introuvable.");

        var updated = floor.Update(command.Code, command.Name, command.Level, command.DisplayOrder, command.Description);
        if (updated.IsFailure) return updated;

        if (await floors.CodeExistsAsync(floor.BuildingId, floor.Code, floor.Id, cancellationToken))
            return Result.Failure($"Le code '{floor.Code}' est déjà utilisé dans ce bâtiment.");

        await floors.UpdateAsync(floor, cancellationToken);
        return Result.Success();
    }
}
