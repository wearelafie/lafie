using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Waste.Collections;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Expédie un bordereau : fige le poids total cumulé des collectes rattachées.</summary>
public sealed record ShipWasteSlipCommand(Guid Id) : IRequest<Result>;

public sealed class ShipWasteSlipHandler(
    IWasteSlipRepository slips,
    IWasteCollectionRepository collections) : IRequestHandler<ShipWasteSlipCommand, Result>
{
    public async ValueTask<Result> Handle(ShipWasteSlipCommand command, CancellationToken cancellationToken)
    {
        var slip = await slips.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bordereau {command.Id} introuvable.");

        var ids = await collections.GetSlipCollectionIdsAsync(slip.Id, cancellationToken);
        if (ids.Count == 0) return Result.Failure("Rattachez au moins une collecte avant d'expédier le bordereau.");

        var totalWeight = await collections.SumWeightForSlipAsync(slip.Id, cancellationToken);
        var result = slip.Ship(totalWeight);
        if (result.IsFailure) return result;

        await slips.UpdateAsync(slip, cancellationToken);
        return Result.Success();
    }
}
