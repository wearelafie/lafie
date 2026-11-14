using Lafie.Core.Common;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Movements;

/// <summary>Enregistre un déplacement d'équipement et met à jour sa localisation courante.</summary>
public sealed record CreateEquipmentMovementCommand(
    Guid EquipmentId, DateOnly MovedOn, Guid? FromSpaceId, Guid? ToSpaceId,
    string? Reason, string? PerformedBy, string? Notes, bool UpdateCurrentLocation = true)
    : IRequest<Result<Guid>>;

public sealed class CreateEquipmentMovementHandler(
    IEquipmentMovementRepository movements,
    IEquipmentRepository equipment,
    ISpaceRepository spaces) : IRequestHandler<CreateEquipmentMovementCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateEquipmentMovementCommand command, CancellationToken cancellationToken)
    {
        var item = await equipment.FindAsync(command.EquipmentId, cancellationToken);
        if (item is null) return Result.Failure<Guid>("Équipement introuvable.");

        foreach (var spaceId in new[] { command.FromSpaceId, command.ToSpaceId })
        {
            if (spaceId is { } sp && sp != Guid.Empty)
            {
                var space = await spaces.GetByIdAsync(sp, cancellationToken);
                if (space is null) return Result.Failure<Guid>("Espace introuvable.");
                if (space.FacilityId != item.FacilityId)
                    return Result.Failure<Guid>("L'espace appartient à un autre établissement.");
            }
        }

        // Par défaut, l'origine est la localisation actuelle de l'équipement.
        var fromSpaceId = command.FromSpaceId ?? item.SpaceId;

        var created = EquipmentMovement.Create(
            command.EquipmentId, command.MovedOn, fromSpaceId, command.ToSpaceId,
            command.Reason, command.PerformedBy, command.Notes);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await movements.AddAsync(created.Value, cancellationToken);

        if (command.UpdateCurrentLocation && command.ToSpaceId is { } to && to != Guid.Empty && item.SpaceId != to)
        {
            item.Relocate(to);
            await equipment.UpdateAsync(item, cancellationToken);
        }

        return Result.Success(created.Value.Id);
    }
}
