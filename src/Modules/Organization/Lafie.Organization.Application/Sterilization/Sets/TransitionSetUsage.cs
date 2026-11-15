using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Fait avancer une affectation : use | return | cancel, et synchronise l'état de la boîte.</summary>
public sealed record TransitionSetUsageCommand(Guid UsageId, string Action) : IRequest<Result>;

public sealed class TransitionSetUsageHandler(IInstrumentSetRepository sets)
    : IRequestHandler<TransitionSetUsageCommand, Result>
{
    public async ValueTask<Result> Handle(TransitionSetUsageCommand command, CancellationToken cancellationToken)
    {
        var usage = await sets.FindUsageAsync(command.UsageId, cancellationToken)
            ?? throw new NotFoundException($"Affectation {command.UsageId} introuvable.");

        var action = command.Action.ToLowerInvariant();
        var result = action switch
        {
            "use" => usage.MarkUsed(),
            "return" => usage.Return(),
            "cancel" => usage.Cancel(),
            _ => Result.Failure($"Action inconnue : '{command.Action}'."),
        };
        if (result.IsFailure) return result;

        await sets.UpdateUsageAsync(usage, cancellationToken);

        // Synchronisation de l'état de la boîte : retour d'intervention → souillée ; annulation → redevient stérile.
        if (action is "return" or "cancel")
        {
            var set = await sets.FindAsync(usage.SetId, cancellationToken);
            if (set is not null && set.Status is InstrumentSetStatus.InUse)
            {
                if (action is "return") set.MarkSoiled();
                else set.RestoreSterile();
                await sets.UpdateAsync(set, cancellationToken);
            }
        }

        return Result.Success();
    }
}
