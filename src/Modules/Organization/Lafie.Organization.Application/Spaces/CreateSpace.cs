using Lafie.Core.Common;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Application.Floors;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Spaces;

/// <summary>Crée un espace dans un étage. L'établissement/bâtiment sont dérivés de l'étage (cohérence garantie).</summary>
public sealed record CreateSpaceCommand(
    Guid FloorId, string Code, string Name, string Type,
    string? Description = null, decimal? Area = null, int? MaxCapacity = null, bool Accessible = false)
    : IRequest<Result<Guid>>;

public sealed class CreateSpaceHandler(
    ISpaceRepository spaces,
    IFloorRepository floors,
    IBuildingRepository buildings) : IRequestHandler<CreateSpaceCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateSpaceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SpaceType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure<Guid>($"Type d'espace inconnu : '{command.Type}'.");

        var floor = await floors.GetByIdAsync(command.FloorId, cancellationToken);
        if (floor is null) return Result.Failure<Guid>("Étage introuvable.");

        var building = await buildings.GetByIdAsync(floor.BuildingId, cancellationToken);
        if (building is null) return Result.Failure<Guid>("Bâtiment introuvable.");

        var created = Space.Create(
            building.FacilityId, floor.BuildingId, command.FloorId, command.Code, command.Name, type,
            command.Description, command.Area, command.MaxCapacity, command.Accessible);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await spaces.CodeExistsAsync(building.FacilityId, created.Value.Code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code '{created.Value.Code}' est déjà utilisé dans cet établissement.");

        await spaces.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
