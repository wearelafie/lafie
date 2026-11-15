using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Waste.Collections;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>
/// Marque un bordereau expédié comme traité : chaque collecte rattachée passe à « éliminée »
/// (clôture de la traçabilité réglementaire).
/// </summary>
public sealed record MarkSlipTreatedCommand(Guid Id, string? Treatment) : IRequest<Result>;

public sealed class MarkSlipTreatedHandler(
    IWasteSlipRepository slips,
    IWasteCollectionRepository collections) : IRequestHandler<MarkSlipTreatedCommand, Result>
{
    public async ValueTask<Result> Handle(MarkSlipTreatedCommand command, CancellationToken cancellationToken)
    {
        if (!WasteTreatmentParser.TryParse(command.Treatment, out var treatment))
            return Result.Failure($"Traitement inconnu : '{command.Treatment}'.");

        var slip = await slips.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bordereau {command.Id} introuvable.");

        var result = slip.MarkTreated(treatment);
        if (result.IsFailure) return result;

        await slips.UpdateAsync(slip, cancellationToken);

        foreach (var id in await collections.GetSlipCollectionIdsAsync(slip.Id, cancellationToken))
        {
            var collection = await collections.FindAsync(id, cancellationToken);
            if (collection is null) continue;
            collection.MarkEliminated();
            await collections.UpdateAsync(collection, cancellationToken);
        }

        return Result.Success();
    }
}
