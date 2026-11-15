using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Fait avancer une tâche : start | complete | cancel. Libère l'espace quand plus aucune tâche active ne subsiste.</summary>
public sealed record TransitionCleaningTaskCommand(Guid Id, string Action) : IRequest<Result>;

public sealed class TransitionCleaningTaskHandler(
    ICleaningTaskRepository tasks,
    ISpaceRepository spaces) : IRequestHandler<TransitionCleaningTaskCommand, Result>
{
    public async ValueTask<Result> Handle(TransitionCleaningTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await tasks.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Tâche {command.Id} introuvable.");

        var action = command.Action.ToLowerInvariant();
        var result = action switch
        {
            "start" => task.Start(),
            "complete" => task.Complete(await tasks.AllItemsDoneAsync(task.Id, null, cancellationToken)),
            "cancel" => task.Cancel(),
            _ => Result.Failure($"Action inconnue : '{command.Action}'."),
        };
        if (result.IsFailure) return result;

        await tasks.UpdateAsync(task, cancellationToken);

        // Libération : si la tâche n'occupe plus l'espace et qu'aucune autre tâche active ne subsiste,
        // remettre l'espace en Available — sauf s'il est verrouillé par une maintenance (Blocked prioritaire).
        if (!task.IsActive)
        {
            var remaining = await tasks.CountActiveForSpaceAsync(task.SpaceId, task.Id, cancellationToken);
            if (remaining == 0)
            {
                var space = await spaces.FindAsync(task.SpaceId, cancellationToken);
                if (space is not null && space.OperationalState == SpaceOperationalState.Cleaning)
                {
                    space.SetOperationalState(SpaceOperationalState.Available);
                    await spaces.UpdateAsync(space, cancellationToken);
                }
            }
        }

        return Result.Success();
    }
}
