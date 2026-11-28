using Lafie.Core.Common;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Référence un nouveau médicament au catalogue d'un établissement.</summary>
public sealed record CreateMedicationCommand(
    Guid FacilityId, string Code, string Name, string? Inn, MedicationForm Form, string? Strength,
    string Unit, string? AtcCode, string? TherapeuticClass, bool Controlled, string? Notes) : IRequest<Result<Guid>>;

public sealed class CreateMedicationHandler(IMedicationRepository medications)
    : IRequestHandler<CreateMedicationCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateMedicationCommand command, CancellationToken cancellationToken)
    {
        var code = command.Code?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(code) &&
            await medications.CodeExistsAsync(command.FacilityId, code, null, cancellationToken))
            return Result.Failure<Guid>($"Le code « {code} » existe déjà dans ce catalogue.");

        var created = Medication.Create(
            command.FacilityId, command.Code, command.Name, command.Inn, command.Form, command.Strength,
            command.Unit, command.AtcCode, command.TherapeuticClass, command.Controlled, command.Notes);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await medications.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
