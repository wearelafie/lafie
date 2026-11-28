using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Prescription de médicaments pour un patient. Le patient est identifié en texte libre
/// (nom + référence optionnelle) en attendant le module Patient ; <see cref="PatientId"/> est
/// réservé pour le rattachement futur. Les lignes sont gérées séparément (<see cref="PrescriptionLine"/>).
/// La dispensation d'une ligne décrémente le stock (sortie FEFO).
/// </summary>
public sealed class Prescription : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int ReferenceMaxLength = 50;
    public const int NotesMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public Guid? PatientId { get; private set; }           // réservé (module Patient à venir)
    public string PatientName { get; private set; } = string.Empty;
    public string? PatientReference { get; private set; }
    public string? PrescriberName { get; private set; }
    public string? Reference { get; private set; }
    public PrescriptionStatus Status { get; private set; }
    public DateOnly? PrescribedOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private Prescription(
        Guid id, Guid facilityId, Guid? patientId, string patientName, string? patientReference,
        string? prescriberName, string? reference, PrescriptionStatus status, DateOnly? prescribedOn,
        string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        PatientId = patientId;
        PatientName = patientName;
        PatientReference = patientReference;
        PrescriberName = prescriberName;
        Reference = reference;
        Status = status;
        PrescribedOn = prescribedOn;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<Prescription> Create(
        Guid facilityId, Guid? patientId, string patientName, string? patientReference,
        string? prescriberName, string? reference, DateOnly? prescribedOn, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<Prescription>("Établissement obligatoire.");
        if (string.IsNullOrWhiteSpace(patientName)) return Result.Failure<Prescription>("Le nom du patient est obligatoire.");

        return Result.Success(new Prescription(
            Guid.NewGuid(), facilityId, patientId, Clamp(patientName, NameMaxLength)!, Clamp(patientReference, NameMaxLength),
            Clamp(prescriberName, NameMaxLength), Clamp(reference, ReferenceMaxLength), PrescriptionStatus.Active,
            prescribedOn ?? DateOnly.FromDateTime(DateTime.UtcNow), Clamp(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null));
    }

    public Result Cancel()
    {
        if (Status is PrescriptionStatus.Completed or PrescriptionStatus.Cancelled)
            return Result.Failure("Cette prescription ne peut plus être annulée.");
        Status = PrescriptionStatus.Cancelled;
        Touch();
        return Result.Success();
    }

    /// <summary>Met à jour le statut après dispensation. <paramref name="allDispensed"/> : toutes les lignes sont servies.</summary>
    public Result MarkDispensing(bool allDispensed)
    {
        if (Status != PrescriptionStatus.Active)
            return Result.Failure("Seule une prescription active peut être dispensée.");
        if (allDispensed) { Status = PrescriptionStatus.Completed; Touch(); }
        else Touch();
        return Result.Success();
    }

    public static Prescription Rehydrate(
        Guid id, Guid facilityId, Guid? patientId, string patientName, string? patientReference,
        string? prescriberName, string? reference, PrescriptionStatus status, DateOnly? prescribedOn,
        string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, patientId, patientName, patientReference, prescriberName, reference, status,
            prescribedOn, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
