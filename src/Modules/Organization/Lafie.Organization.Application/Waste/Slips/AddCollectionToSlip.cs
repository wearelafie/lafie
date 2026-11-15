using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Waste.Collections;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Rattache une collecte à un bordereau au brouillon (même établissement, même filière).</summary>
public sealed record AddCollectionToSlipCommand(Guid SlipId, Guid CollectionId) : IRequest<Result>;

public sealed class AddCollectionToSlipHandler(
    IWasteSlipRepository slips,
    IWasteCollectionRepository collections) : IRequestHandler<AddCollectionToSlipCommand, Result>
{
    public async ValueTask<Result> Handle(AddCollectionToSlipCommand command, CancellationToken cancellationToken)
    {
        var slip = await slips.FindAsync(command.SlipId, cancellationToken)
            ?? throw new NotFoundException($"Bordereau {command.SlipId} introuvable.");
        if (!slip.IsDraft) return Result.Failure("Seul un bordereau au brouillon accepte de nouvelles collectes.");

        var collection = await collections.FindAsync(command.CollectionId, cancellationToken)
            ?? throw new NotFoundException($"Collecte {command.CollectionId} introuvable.");
        if (collection.FacilityId != slip.FacilityId) return Result.Failure("La collecte n'appartient pas à cet établissement.");
        if (collection.Stream != slip.Stream) return Result.Failure("La filière de la collecte diffère de celle du bordereau.");

        var result = collection.AttachToSlip(slip.Id);
        if (result.IsFailure) return result;

        await collections.UpdateAsync(collection, cancellationToken);
        return Result.Success();
    }
}
