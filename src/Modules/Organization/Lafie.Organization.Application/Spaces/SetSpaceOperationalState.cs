using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Spaces;

/// <summary>Change l'état opérationnel runtime d'un espace (Available/Occupied/Cleaning/Blocked).</summary>
public sealed record SetSpaceOperationalStateCommand(Guid Id, string State) : IRequest<Result>;

public sealed class SetSpaceOperationalStateHandler(ISpaceRepository spaces) : IRequestHandler<SetSpaceOperationalStateCommand, Result>
{
    public async ValueTask<Result> Handle(SetSpaceOperationalStateCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SpaceOperationalState>(command.State, ignoreCase: true, out var state))
            return Result.Failure($"État opérationnel inconnu : '{command.State}'.");

        var space = await spaces.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Espace {command.Id} introuvable.");

        space.SetOperationalState(state);
        await spaces.UpdateAsync(space, cancellationToken);
        return Result.Success();
    }
}
