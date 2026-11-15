using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Démarre un cycle planifié (verrouille sa charge).</summary>
public sealed record StartSterilizationCycleCommand(Guid Id) : IRequest<Result>;

public sealed class StartSterilizationCycleHandler(ISterilizationCycleRepository cycles)
    : IRequestHandler<StartSterilizationCycleCommand, Result>
{
    public async ValueTask<Result> Handle(StartSterilizationCycleCommand command, CancellationToken cancellationToken)
    {
        var cycle = await cycles.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Cycle {command.Id} introuvable.");

        var load = await cycles.GetLoadSetIdsAsync(cycle.Id, cancellationToken);
        if (load.Count == 0) return Result.Failure("Ajoutez au moins une boîte à la charge avant de démarrer.");

        var result = cycle.Start();
        if (result.IsFailure) return result;

        await cycles.UpdateAsync(cycle, cancellationToken);
        return Result.Success();
    }
}
