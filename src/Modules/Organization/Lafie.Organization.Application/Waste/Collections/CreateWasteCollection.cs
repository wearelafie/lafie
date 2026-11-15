using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Waste.Collections;

/// <summary>Enregistre une collecte pesée de déchets.</summary>
public sealed record CreateWasteCollectionCommand(
    Guid FacilityId, string Stream, decimal WeightKg, int ContainerCount,
    Guid? SpaceId, DateOnly? CollectedOn, string? CollectedBy, string? Notes) : IRequest<Result<Guid>>;

public sealed class CreateWasteCollectionHandler(IWasteCollectionRepository collections)
    : IRequestHandler<CreateWasteCollectionCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateWasteCollectionCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WasteStream>(command.Stream, ignoreCase: true, out var stream))
            return Result.Failure<Guid>($"Filière inconnue : '{command.Stream}'.");

        var result = WasteCollection.Create(
            command.FacilityId, stream, command.WeightKg, command.ContainerCount,
            command.SpaceId, command.CollectedOn, command.CollectedBy, command.Notes);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error!);

        await collections.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id);
    }
}
