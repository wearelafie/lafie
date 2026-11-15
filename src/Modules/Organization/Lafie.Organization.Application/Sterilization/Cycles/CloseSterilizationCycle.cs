using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Sterilization.Sets;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>
/// Clôture un cycle avec son résultat et propage l'état à toute la charge :
/// réussite → boîtes stériles (datées + péremption) ; échec → boîtes en quarantaine.
/// </summary>
public sealed record CloseSterilizationCycleCommand(
    Guid Id, bool Passed, decimal? TemperatureC, decimal? PressureBar, int? DurationMin, string? ResultNotes) : IRequest<Result>;

public sealed class CloseSterilizationCycleHandler(
    ISterilizationCycleRepository cycles,
    IInstrumentSetRepository sets) : IRequestHandler<CloseSterilizationCycleCommand, Result>
{
    /// <summary>Durée de validité de la stérilité (péremption) par défaut, à défaut de gestion par conditionnement.</summary>
    private const int DefaultSterilityDays = 30;

    public async ValueTask<Result> Handle(CloseSterilizationCycleCommand command, CancellationToken cancellationToken)
    {
        var cycle = await cycles.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Cycle {command.Id} introuvable.");

        var result = cycle.Close(command.Passed, command.TemperatureC, command.PressureBar, command.DurationMin, command.ResultNotes);
        if (result.IsFailure) return result;

        await cycles.UpdateAsync(cycle, cancellationToken);

        var end = cycle.EndedOnUtc ?? DateTimeOffset.UtcNow;
        var expiry = DateOnly.FromDateTime(end.UtcDateTime).AddDays(DefaultSterilityDays);

        foreach (var setId in await cycles.GetLoadSetIdsAsync(cycle.Id, cancellationToken))
        {
            var set = await sets.FindAsync(setId, cancellationToken);
            if (set is null) continue;
            if (command.Passed) set.MarkSterilized(cycle.Id, end, expiry);
            else set.MarkQuarantine(cycle.Id);
            await sets.UpdateAsync(set, cancellationToken);
        }

        return Result.Success();
    }
}
