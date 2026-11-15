using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Waste.Collections;

/// <summary>Modifie une collecte non encore rattachée à un bordereau.</summary>
public sealed record UpdateWasteCollectionCommand(
    Guid Id, string Stream, decimal WeightKg, int ContainerCount,
    Guid? SpaceId, DateOnly? CollectedOn, string? CollectedBy, string? Notes) : IRequest<Result>;

public sealed class UpdateWasteCollectionHandler(IWasteCollectionRepository collections)
    : IRequestHandler<UpdateWasteCollectionCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateWasteCollectionCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WasteStream>(command.Stream, ignoreCase: true, out var stream))
            return Result.Failure($"Filière inconnue : '{command.Stream}'.");

        var collection = await collections.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Collecte {command.Id} introuvable.");

        var result = collection.Edit(
            stream, command.WeightKg, command.ContainerCount,
            command.SpaceId, command.CollectedOn, command.CollectedBy, command.Notes);
        if (result.IsFailure) return result;

        await collections.UpdateAsync(collection, cancellationToken);
        return Result.Success();
    }
}
