using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Référence (Active) ou déréférence (Inactive) une fiche médicament.</summary>
public sealed record SetMedicationStatusCommand(Guid Id, bool Active) : IRequest<Result>;

public sealed class SetMedicationStatusHandler(IMedicationRepository medications)
    : IRequestHandler<SetMedicationStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetMedicationStatusCommand command, CancellationToken cancellationToken)
    {
        var medication = await medications.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Médicament {command.Id} introuvable.");

        var result = medication.SetActive(command.Active);
        if (result.IsFailure) return result;

        await medications.UpdateAsync(medication, cancellationToken);
        return Result.Success();
    }
}
