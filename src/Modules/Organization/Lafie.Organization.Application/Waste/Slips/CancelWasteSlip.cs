using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Waste.Collections;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Annule un bordereau (non traité) et détache ses collectes (retour à « collectée »).</summary>
public sealed record CancelWasteSlipCommand(Guid Id) : IRequest<Result>;

public sealed class CancelWasteSlipHandler(
    IWasteSlipRepository slips,
    IWasteCollectionRepository collections) : IRequestHandler<CancelWasteSlipCommand, Result>
{
    public async ValueTask<Result> Handle(CancelWasteSlipCommand command, CancellationToken cancellationToken)
    {
        var slip = await slips.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bordereau {command.Id} introuvable.");

        var result = slip.Cancel();
        if (result.IsFailure) return result;

        await slips.UpdateAsync(slip, cancellationToken);

        foreach (var id in await collections.GetSlipCollectionIdsAsync(slip.Id, cancellationToken))
        {
            var collection = await collections.FindAsync(id, cancellationToken);
            if (collection is null) continue;
            collection.DetachFromSlip();
            await collections.UpdateAsync(collection, cancellationToken);
        }

        return Result.Success();
    }
}
