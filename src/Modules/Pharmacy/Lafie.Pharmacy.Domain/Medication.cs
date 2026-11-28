using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Fiche médicament du catalogue d'un établissement : référentiel des produits pharmaceutiques
/// (DCI, forme galénique, dosage, unité de dispensation). Le stock, les lots et la dispensation
/// sont gérés par des agrégats distincts qui référencent cette fiche par son identifiant.
/// </summary>
public sealed class Medication : AggregateRoot<Guid>
{
    public const int CodeMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int ShortMaxLength = 100;
    public const int NotesMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Inn { get; private set; }
    public MedicationForm Form { get; private set; }
    public string? Strength { get; private set; }
    public string Unit { get; private set; }
    public string? AtcCode { get; private set; }
    public string? TherapeuticClass { get; private set; }
    public bool Controlled { get; private set; }
    public MedicationStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private Medication(
        Guid id, Guid facilityId, string code, string name, string? inn, MedicationForm form, string? strength,
        string unit, string? atcCode, string? therapeuticClass, bool controlled, MedicationStatus status,
        string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Code = code;
        Name = name;
        Inn = inn;
        Form = form;
        Strength = strength;
        Unit = unit;
        AtcCode = atcCode;
        TherapeuticClass = therapeuticClass;
        Controlled = controlled;
        Status = status;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<Medication> Create(
        Guid facilityId, string code, string name, string? inn, MedicationForm form, string? strength,
        string unit, string? atcCode, string? therapeuticClass, bool controlled, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<Medication>("Établissement obligatoire.");
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<Medication>("Le code est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<Medication>("Le nom est obligatoire.");
        if (string.IsNullOrWhiteSpace(unit)) return Result.Failure<Medication>("L'unité de dispensation est obligatoire.");

        var medication = new Medication(
            Guid.NewGuid(), facilityId, Clamp(code, CodeMaxLength)!, Clamp(name, NameMaxLength)!, Clamp(inn, NameMaxLength),
            form, Clamp(strength, ShortMaxLength), Clamp(unit, ShortMaxLength)!, Clamp(atcCode, ShortMaxLength),
            Clamp(therapeuticClass, NameMaxLength), controlled, MedicationStatus.Active, Clamp(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null);
        return Result.Success(medication);
    }

    /// <summary>Modifie les informations descriptives (le code et l'établissement sont immuables).</summary>
    public Result Edit(
        string name, string? inn, MedicationForm form, string? strength, string unit,
        string? atcCode, string? therapeuticClass, bool controlled, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure("Le nom est obligatoire.");
        if (string.IsNullOrWhiteSpace(unit)) return Result.Failure("L'unité de dispensation est obligatoire.");

        Name = Clamp(name, NameMaxLength)!;
        Inn = Clamp(inn, NameMaxLength);
        Form = form;
        Strength = Clamp(strength, ShortMaxLength);
        Unit = Clamp(unit, ShortMaxLength)!;
        AtcCode = Clamp(atcCode, ShortMaxLength);
        TherapeuticClass = Clamp(therapeuticClass, NameMaxLength);
        Controlled = controlled;
        Notes = Clamp(notes, NotesMaxLength);
        Touch();
        return Result.Success();
    }

    public Result SetActive(bool active)
    {
        var target = active ? MedicationStatus.Active : MedicationStatus.Inactive;
        if (Status == target) return Result.Success();
        Status = target;
        Touch();
        return Result.Success();
    }

    public static Medication Rehydrate(
        Guid id, Guid facilityId, string code, string name, string? inn, MedicationForm form, string? strength,
        string unit, string? atcCode, string? therapeuticClass, bool controlled, MedicationStatus status,
        string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, code, name, inn, form, strength, unit, atcCode, therapeuticClass, controlled, status,
            notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
