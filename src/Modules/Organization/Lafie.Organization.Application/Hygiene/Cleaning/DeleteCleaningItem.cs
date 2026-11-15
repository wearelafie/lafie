using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Retire une étape de la checklist.</summary>
public sealed record DeleteCleaningItemCommand(Guid ItemId) : IRequest<Result>;

public sealed class DeleteCleaningItemHandler(ICleaningTaskRepository tasks)
    : IRequestHandler<DeleteCleaningItemCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteCleaningItemCommand command, CancellationToken cancellationToken)
    {
        await tasks.DeleteItemAsync(command.ItemId, cancellationToken);
        return Result.Success();
    }
}
