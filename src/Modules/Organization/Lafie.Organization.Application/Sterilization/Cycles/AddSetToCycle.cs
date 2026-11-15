using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Sterilization.Sets;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Ajoute une boîte à la charge d'un cycle planifié (la boîte entre en stérilisation).</summary>
public sealed record AddSetToCycleCommand(Guid CycleId, Guid SetId) : IRequest<Result>;

public sealed class AddSetToCycleHandler(
    ISterilizationCycleRepository cycles,
    IInstrumentSetRepository sets) : IRequestHandler<AddSetToCycleCommand, Result>
{
    public async ValueTask<Result> Handle(AddSetToCycleCommand command, CancellationToken cancellationToken)
    {
        var cycle = await cycles.FindAsync(command.CycleId, cancellationToken)
            ?? throw new NotFoundException($"Cycle {command.CycleId} introuvable.");
        if (cycle.Status is not SterilizationCycleStatus.Planned)
            return Result.Failure("La charge ne peut être modifiée qu'avant le démarrage du cycle.");

        var set = await sets.FindAsync(command.SetId, cancellationToken);
        if (set is null || set.FacilityId != cycle.FacilityId) return Result.Failure("Boîte introuvable.");
        if (await cycles.IsInLoadAsync(cycle.Id, set.Id, cancellationToken)) return Result.Success();

        var place = set.PlaceInSterilization();
        if (place.IsFailure) return place;

        await cycles.AddToLoadAsync(cycle.Id, set.Id, cancellationToken);
        await sets.UpdateAsync(set, cancellationToken);
        return Result.Success();
    }
}
