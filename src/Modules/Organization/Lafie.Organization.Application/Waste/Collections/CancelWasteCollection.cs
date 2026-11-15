using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Waste.Collections;

/// <summary>Annule une collecte (détache d'un éventuel bordereau au brouillon).</summary>
public sealed record CancelWasteCollectionCommand(Guid Id) : IRequest<Result>;

public sealed class CancelWasteCollectionHandler(IWasteCollectionRepository collections)
    : IRequestHandler<CancelWasteCollectionCommand, Result>
{
    public async ValueTask<Result> Handle(CancelWasteCollectionCommand command, CancellationToken cancellationToken)
    {
        var collection = await collections.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Collecte {command.Id} introuvable.");

        var result = collection.Cancel();
        if (result.IsFailure) return result;

        await collections.UpdateAsync(collection, cancellationToken);
        return Result.Success();
    }
}
