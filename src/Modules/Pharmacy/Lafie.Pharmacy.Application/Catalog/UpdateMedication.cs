using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Modifie les informations descriptives d'une fiche médicament (code immuable).</summary>
public sealed record UpdateMedicationCommand(
    Guid Id, string Name, string? Inn, MedicationForm Form, string? Strength, string Unit,
    string? AtcCode, string? TherapeuticClass, bool Controlled, string? Notes) : IRequest<Result>;

public sealed class UpdateMedicationHandler(IMedicationRepository medications)
    : IRequestHandler<UpdateMedicationCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateMedicationCommand command, CancellationToken cancellationToken)
    {
        var medication = await medications.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Médicament {command.Id} introuvable.");

        var result = medication.Edit(
            command.Name, command.Inn, command.Form, command.Strength, command.Unit,
            command.AtcCode, command.TherapeuticClass, command.Controlled, command.Notes);
        if (result.IsFailure) return result;

        await medications.UpdateAsync(medication, cancellationToken);
        return Result.Success();
    }
}
