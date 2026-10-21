using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Spaces;

/// <summary>Change l'état opérationnel d'un espace (dont indisponibilité temporaire).</summary>
public sealed record SetSpaceStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetSpaceStatusHandler(ISpaceRepository spaces) : IRequestHandler<SetSpaceStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetSpaceStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SpaceStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"Statut inconnu : '{command.Status}'.");

        var space = await spaces.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Espace {command.Id} introuvable.");

        space.SetStatus(status);
        await spaces.UpdateAsync(space, cancellationToken);
        return Result.Success();
    }
}
