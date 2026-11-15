using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Crée un bordereau d'élimination (brouillon).</summary>
public sealed record CreateWasteSlipCommand(
    Guid FacilityId, string Reference, string Stream, string? Carrier, string? Destination, string? Treatment) : IRequest<Result<Guid>>;

public sealed class CreateWasteSlipHandler(IWasteSlipRepository slips)
    : IRequestHandler<CreateWasteSlipCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateWasteSlipCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WasteStream>(command.Stream, ignoreCase: true, out var stream))
            return Result.Failure<Guid>($"Filière inconnue : '{command.Stream}'.");
        if (!WasteTreatmentParser.TryParse(command.Treatment, out var treatment))
            return Result.Failure<Guid>($"Traitement inconnu : '{command.Treatment}'.");

        if (await slips.ReferenceExistsAsync(command.FacilityId, command.Reference.Trim(), null, cancellationToken))
            return Result.Failure<Guid>("Un bordereau avec cette référence existe déjà.");

        var result = WasteEliminationSlip.Create(
            command.FacilityId, command.Reference, stream, command.Carrier, command.Destination, treatment);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error!);

        await slips.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id);
    }
}
