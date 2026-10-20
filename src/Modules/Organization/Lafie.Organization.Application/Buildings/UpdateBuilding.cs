using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Buildings;

/// <summary>Met à jour un bâtiment.</summary>
public sealed record UpdateBuildingCommand(
    Guid Id, string Code, string Name, string? Description = null, string? Location = null)
    : IRequest<Result>;

public sealed class UpdateBuildingHandler(IBuildingRepository buildings)
    : IRequestHandler<UpdateBuildingCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateBuildingCommand command, CancellationToken cancellationToken)
    {
        var building = await buildings.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bâtiment {command.Id} introuvable.");

        var updated = building.Update(command.Code, command.Name, command.Description, command.Location);
        if (updated.IsFailure) return updated;

        if (await buildings.CodeExistsAsync(building.FacilityId, building.Code, building.Id, cancellationToken))
            return Result.Failure($"Le code '{building.Code}' est déjà utilisé dans cet établissement.");

        await buildings.UpdateAsync(building, cancellationToken);
        return Result.Success();
    }
}
