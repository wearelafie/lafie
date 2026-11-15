using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Waste.Collections;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Retire une collecte d'un bordereau au brouillon (retour à l'état « collectée »).</summary>
public sealed record RemoveCollectionFromSlipCommand(Guid SlipId, Guid CollectionId) : IRequest<Result>;

public sealed class RemoveCollectionFromSlipHandler(
    IWasteSlipRepository slips,
    IWasteCollectionRepository collections) : IRequestHandler<RemoveCollectionFromSlipCommand, Result>
{
    public async ValueTask<Result> Handle(RemoveCollectionFromSlipCommand command, CancellationToken cancellationToken)
    {
        var slip = await slips.FindAsync(command.SlipId, cancellationToken)
            ?? throw new NotFoundException($"Bordereau {command.SlipId} introuvable.");
        if (!slip.IsDraft) return Result.Failure("Seul un bordereau au brouillon peut être modifié.");

        var collection = await collections.FindAsync(command.CollectionId, cancellationToken)
            ?? throw new NotFoundException($"Collecte {command.CollectionId} introuvable.");
        if (collection.SlipId != slip.Id) return Result.Failure("La collecte n'est pas rattachée à ce bordereau.");

        var result = collection.DetachFromSlip();
        if (result.IsFailure) return result;

        await collections.UpdateAsync(collection, cancellationToken);
        return Result.Success();
    }
}
