using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Spaces;

/// <summary>Met à jour un espace.</summary>
public sealed record UpdateSpaceCommand(
    Guid Id, string Code, string Name, string Type,
    string? Description = null, decimal? Area = null, int? MaxCapacity = null, bool Accessible = false)
    : IRequest<Result>;

public sealed class UpdateSpaceHandler(ISpaceRepository spaces) : IRequestHandler<UpdateSpaceCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateSpaceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SpaceType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure($"Type d'espace inconnu : '{command.Type}'.");

        var space = await spaces.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Espace {command.Id} introuvable.");

        var updated = space.Update(command.Code, command.Name, type, command.Description, command.Area, command.MaxCapacity, command.Accessible);
        if (updated.IsFailure) return updated;

        if (await spaces.CodeExistsAsync(space.FacilityId, space.Code, space.Id, cancellationToken))
            return Result.Failure($"Le code '{space.Code}' est déjà utilisé dans cet établissement.");

        await spaces.UpdateAsync(space, cancellationToken);
        return Result.Success();
    }
}
