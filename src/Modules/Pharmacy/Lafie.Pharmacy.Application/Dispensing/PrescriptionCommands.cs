using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Mediator;

namespace Lafie.Pharmacy.Application.Dispensing;

/// <summary>Crée une prescription active avec ses lignes.</summary>
public sealed record CreatePrescriptionCommand(
    Guid FacilityId, string PatientName, string? PatientReference, string? PrescriberName, string? Reference,
    DateOnly? PrescribedOn, string? Notes, IReadOnlyList<PrescriptionLineInput> Lines) : IRequest<Result<Guid>>;

public sealed class CreatePrescriptionHandler(IPrescriptionRepository prescriptions, IMedicationRepository medications)
    : IRequestHandler<CreatePrescriptionCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreatePrescriptionCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0) return Result.Failure<Guid>("Une prescription doit comporter au moins une ligne.");

        var header = Prescription.Create(command.FacilityId, patientId: null, command.PatientName, command.PatientReference,
            command.PrescriberName, command.Reference, command.PrescribedOn, command.Notes);
        if (header.IsFailure) return Result.Failure<Guid>(header.Error!);

        var lines = new List<PrescriptionLine>();
        foreach (var input in command.Lines)
        {
            var med = await medications.GetByIdAsync(input.MedicationId, cancellationToken);
            if (med is null || med.FacilityId != command.FacilityId)
                return Result.Failure<Guid>("Une ligne référence un médicament introuvable dans cet établissement.");

            var line = PrescriptionLine.Create(header.Value.Id, input.MedicationId, input.Dosage, input.QuantityPrescribed);
            if (line.IsFailure) return Result.Failure<Guid>(line.Error!);
            lines.Add(line.Value);
        }

        await prescriptions.AddAsync(header.Value, lines, cancellationToken);
        return Result.Success(header.Value.Id);
    }
}

/// <summary>Annule une prescription (tant qu'elle n'est pas totalement dispensée).</summary>
public sealed record CancelPrescriptionCommand(Guid Id) : IRequest<Result>;

public sealed class CancelPrescriptionHandler(IPrescriptionRepository prescriptions)
    : IRequestHandler<CancelPrescriptionCommand, Result>
{
    public async ValueTask<Result> Handle(CancelPrescriptionCommand command, CancellationToken cancellationToken)
    {
        var prescription = await prescriptions.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Prescription {command.Id} introuvable.");

        var result = prescription.Cancel();
        if (result.IsFailure) return result;

        await prescriptions.UpdateHeaderAsync(prescription, cancellationToken);
        return Result.Success();
    }
}
