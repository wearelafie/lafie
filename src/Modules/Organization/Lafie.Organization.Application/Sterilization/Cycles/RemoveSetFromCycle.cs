using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Sterilization.Sets;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Retire une boîte de la charge d'un cycle planifié (la boîte redevient souillée).</summary>
public sealed record RemoveSetFromCycleCommand(Guid CycleId, Guid SetId) : IRequest<Result>;

public sealed class RemoveSetFromCycleHandler(
    ISterilizationCycleRepository cycles,
    IInstrumentSetRepository sets) : IRequestHandler<RemoveSetFromCycleCommand, Result>
{
    public async ValueTask<Result> Handle(RemoveSetFromCycleCommand command, CancellationToken cancellationToken)
    {
        var cycle = await cycles.FindAsync(command.CycleId, cancellationToken)
            ?? throw new NotFoundException($"Cycle {command.CycleId} introuvable.");
        if (cycle.Status is not SterilizationCycleStatus.Planned)
            return Result.Failure("La charge ne peut être modifiée qu'avant le démarrage du cycle.");

        if (!await cycles.IsInLoadAsync(cycle.Id, command.SetId, cancellationToken)) return Result.Success();

        await cycles.RemoveFromLoadAsync(cycle.Id, command.SetId, cancellationToken);

        var set = await sets.FindAsync(command.SetId, cancellationToken);
        if (set is not null && set.Status is InstrumentSetStatus.InSterilization)
        {
            set.RemoveFromSterilization();
            await sets.UpdateAsync(set, cancellationToken);
        }

        return Result.Success();
    }
}
