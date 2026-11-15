using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Affecte une boîte stérile à une intervention (la boîte passe en utilisation).</summary>
public sealed record AllocateSetCommand(
    Guid SetId, string InterventionLabel, string? PatientRef, DateOnly? ScheduledOn, string? PerformedBy, string? Notes) : IRequest<Result<Guid>>;

public sealed class AllocateSetHandler(IInstrumentSetRepository sets)
    : IRequestHandler<AllocateSetCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(AllocateSetCommand command, CancellationToken cancellationToken)
    {
        var set = await sets.FindAsync(command.SetId, cancellationToken)
            ?? throw new NotFoundException($"Boîte {command.SetId} introuvable.");

        var allocate = set.Allocate();
        if (allocate.IsFailure) return Result.Failure<Guid>(allocate.Error!);

        var usage = InstrumentSetUsage.Create(set.Id, command.InterventionLabel, command.PatientRef, command.ScheduledOn, command.PerformedBy, command.Notes);
        if (usage.IsFailure) return Result.Failure<Guid>(usage.Error!);

        await sets.UpdateAsync(set, cancellationToken);
        await sets.AddUsageAsync(usage.Value, cancellationToken);
        return Result.Success(usage.Value.Id);
    }
}
