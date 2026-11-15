using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Modifie l'entête logistique d'un bordereau (transporteur, destination, traitement, notes).</summary>
public sealed record UpdateWasteSlipCommand(
    Guid Id, string? Carrier, string? Destination, string? Treatment, string? Notes) : IRequest<Result>;

public sealed class UpdateWasteSlipHandler(IWasteSlipRepository slips)
    : IRequestHandler<UpdateWasteSlipCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateWasteSlipCommand command, CancellationToken cancellationToken)
    {
        if (!WasteTreatmentParser.TryParse(command.Treatment, out var treatment))
            return Result.Failure($"Traitement inconnu : '{command.Treatment}'.");

        var slip = await slips.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Bordereau {command.Id} introuvable.");

        var result = slip.Edit(command.Carrier, command.Destination, treatment, command.Notes);
        if (result.IsFailure) return result;

        await slips.UpdateAsync(slip, cancellationToken);
        return Result.Success();
    }
}
