using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Affecte une tâche de bio-nettoyage à un agent.</summary>
public sealed record AssignCleaningTaskCommand(Guid Id, string AssignedTo) : IRequest<Result>;

public sealed class AssignCleaningTaskHandler(ICleaningTaskRepository tasks)
    : IRequestHandler<AssignCleaningTaskCommand, Result>
{
    public async ValueTask<Result> Handle(AssignCleaningTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await tasks.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Tâche {command.Id} introuvable.");

        var result = task.Assign(command.AssignedTo);
        if (result.IsFailure) return result;

        await tasks.UpdateAsync(task, cancellationToken);
        return Result.Success();
    }
}
