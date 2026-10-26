using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Spaces;
using Mediator;

namespace Lafie.Organization.Application.CareUnits;

/// <summary>Associe un espace à une unité de soins (M:N). L'espace doit appartenir au même établissement.</summary>
public sealed record AssignSpaceToCareUnitCommand(Guid CareUnitId, Guid SpaceId) : IRequest<Result>;

public sealed class AssignSpaceToCareUnitHandler(ICareUnitRepository careUnits, ISpaceRepository spaces)
    : IRequestHandler<AssignSpaceToCareUnitCommand, Result>
{
    public async ValueTask<Result> Handle(AssignSpaceToCareUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await careUnits.GetByIdAsync(command.CareUnitId, cancellationToken)
            ?? throw new NotFoundException($"Unité de soins {command.CareUnitId} introuvable.");
        var space = await spaces.GetByIdAsync(command.SpaceId, cancellationToken);
        if (space is null) return Result.Failure("Espace introuvable.");
        if (space.FacilityId != unit.FacilityId)
            return Result.Failure("L'espace appartient à un autre établissement.");

        if (!await careUnits.SpaceLinkExistsAsync(command.CareUnitId, command.SpaceId, cancellationToken))
            await careUnits.AddSpaceAsync(command.CareUnitId, command.SpaceId, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Retire l'association entre un espace et une unité de soins.</summary>
public sealed record UnassignSpaceFromCareUnitCommand(Guid CareUnitId, Guid SpaceId) : IRequest<Result>;

public sealed class UnassignSpaceFromCareUnitHandler(ICareUnitRepository careUnits)
    : IRequestHandler<UnassignSpaceFromCareUnitCommand, Result>
{
    public async ValueTask<Result> Handle(UnassignSpaceFromCareUnitCommand command, CancellationToken cancellationToken)
    {
        if (await careUnits.GetByIdAsync(command.CareUnitId, cancellationToken) is null)
            throw new NotFoundException($"Unité de soins {command.CareUnitId} introuvable.");

        await careUnits.RemoveSpaceAsync(command.CareUnitId, command.SpaceId, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Liste les espaces associés à une unité de soins.</summary>
public sealed record GetCareUnitSpacesQuery(Guid CareUnitId) : IRequest<IReadOnlyList<SpaceDto>>;

public sealed class GetCareUnitSpacesHandler(ICareUnitRepository careUnits)
    : IRequestHandler<GetCareUnitSpacesQuery, IReadOnlyList<SpaceDto>>
{
    public async ValueTask<IReadOnlyList<SpaceDto>> Handle(GetCareUnitSpacesQuery query, CancellationToken cancellationToken)
        => await careUnits.GetSpacesAsync(query.CareUnitId, cancellationToken);
}
