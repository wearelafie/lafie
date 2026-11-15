using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Ajoute une étape ad hoc à la checklist d'une tâche.</summary>
public sealed record AddCleaningItemCommand(Guid TaskId, string Label) : IRequest<Result<Guid>>;

public sealed class AddCleaningItemHandler(ICleaningTaskRepository tasks)
    : IRequestHandler<AddCleaningItemCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(AddCleaningItemCommand command, CancellationToken cancellationToken)
    {
        var task = await tasks.FindAsync(command.TaskId, cancellationToken)
            ?? throw new NotFoundException($"Tâche {command.TaskId} introuvable.");

        var sortOrder = await tasks.NextSortOrderAsync(task.Id, cancellationToken);
        var item = CleaningTaskItem.Create(task.Id, command.Label, sortOrder);
        if (item.IsFailure) return Result.Failure<Guid>(item.Error!);

        await tasks.AddItemAsync(item.Value, cancellationToken);
        return Result.Success(item.Value.Id);
    }
}
