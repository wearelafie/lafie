using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Coche ou décoche une étape de la checklist.</summary>
public sealed record ToggleCleaningItemCommand(Guid ItemId, bool Done) : IRequest<Result>;

public sealed class ToggleCleaningItemHandler(ICleaningTaskRepository tasks)
    : IRequestHandler<ToggleCleaningItemCommand, Result>
{
    public async ValueTask<Result> Handle(ToggleCleaningItemCommand command, CancellationToken cancellationToken)
    {
        var item = await tasks.FindItemAsync(command.ItemId, cancellationToken)
            ?? throw new NotFoundException($"Étape {command.ItemId} introuvable.");

        item.SetDone(command.Done);
        await tasks.UpdateItemAsync(item, cancellationToken);
        return Result.Success();
    }
}
