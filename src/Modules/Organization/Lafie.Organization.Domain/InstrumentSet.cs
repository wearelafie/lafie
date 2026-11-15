using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Boîte d'instruments réutilisable (CSSD). Son <see cref="Status"/> suit le circuit de retraitement
/// (souillée → stérilisation → stérile → utilisée → souillée). Les transitions liées aux cycles et aux
/// affectations sont orchestrées par les handlers.
/// </summary>
public sealed class InstrumentSet : AggregateRoot<Guid>
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public int ItemCount { get; private set; }
    public InstrumentSetStatus Status { get; private set; }
    public Guid? LastCycleId { get; private set; }
    public DateTimeOffset? SterilizedOnUtc { get; private set; }
    public DateOnly? ExpiryOn { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private InstrumentSet(
        Guid id, Guid facilityId, string code, string name, string? description, string? category, int itemCount,
        InstrumentSetStatus status, Guid? lastCycleId, DateTimeOffset? sterilizedOnUtc, DateOnly? expiryOn,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Code = code;
        Name = name;
        Description = description;
        Category = category;
        ItemCount = itemCount;
        Status = status;
        LastCycleId = lastCycleId;
        SterilizedOnUtc = sterilizedOnUtc;
        ExpiryOn = expiryOn;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<InstrumentSet> Create(Guid facilityId, string code, string name, string? description, string? category, int itemCount)
    {
        if (facilityId == Guid.Empty) return Result.Failure<InstrumentSet>("Établissement obligatoire.");
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<InstrumentSet>("Le code de la boîte est obligatoire.");
        code = code.Trim();
        if (code.Length > CodeMaxLength) return Result.Failure<InstrumentSet>($"Le code dépasse {CodeMaxLength} caractères.");
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<InstrumentSet>("Le nom de la boîte est obligatoire.");
        name = name.Trim();
        if (name.Length > NameMaxLength) return Result.Failure<InstrumentSet>($"Le nom dépasse {NameMaxLength} caractères.");
        if (itemCount < 0) return Result.Failure<InstrumentSet>("Le nombre d'instruments ne peut pas être négatif.");

        var set = new InstrumentSet(
            Guid.NewGuid(), facilityId, code, name, Trim(description, TextMaxLength), Trim(category, NameMaxLength), itemCount,
            InstrumentSetStatus.Soiled, null, null, null, DateTimeOffset.UtcNow, null);
        return Result.Success(set);
    }

    public Result Edit(string name, string? description, string? category, int itemCount)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure("Le nom de la boîte est obligatoire.");
        name = name.Trim();
        if (name.Length > NameMaxLength) return Result.Failure($"Le nom dépasse {NameMaxLength} caractères.");
        if (itemCount < 0) return Result.Failure("Le nombre d'instruments ne peut pas être négatif.");
        Name = name;
        Description = Trim(description, TextMaxLength);
        Category = Trim(category, NameMaxLength);
        ItemCount = itemCount;
        Touch();
        return Result.Success();
    }

    /// <summary>Entrée en cycle d'autoclave (ajout à une charge).</summary>
    public Result PlaceInSterilization()
    {
        if (Status is not (InstrumentSetStatus.Soiled or InstrumentSetStatus.Quarantine))
            return Result.Failure("Seule une boîte souillée ou en quarantaine peut entrer en stérilisation.");
        Status = InstrumentSetStatus.InSterilization;
        Touch();
        return Result.Success();
    }

    /// <summary>Retrait de la charge avant clôture du cycle.</summary>
    public Result RemoveFromSterilization()
    {
        if (Status is not InstrumentSetStatus.InSterilization) return Result.Failure("La boîte n'est pas en stérilisation.");
        Status = InstrumentSetStatus.Soiled;
        Touch();
        return Result.Success();
    }

    /// <summary>Cycle réussi : la boîte devient stérile et tracée.</summary>
    public void MarkSterilized(Guid cycleId, DateTimeOffset sterilizedOn, DateOnly? expiry)
    {
        Status = InstrumentSetStatus.Sterile;
        LastCycleId = cycleId;
        SterilizedOnUtc = sterilizedOn;
        ExpiryOn = expiry;
        Touch();
    }

    /// <summary>Cycle échoué : la boîte part en quarantaine.</summary>
    public void MarkQuarantine(Guid cycleId)
    {
        Status = InstrumentSetStatus.Quarantine;
        LastCycleId = cycleId;
        Touch();
    }

    /// <summary>Affectation à une intervention : la boîte passe en utilisation.</summary>
    public Result Allocate()
    {
        if (Status is not InstrumentSetStatus.Sterile) return Result.Failure("Seule une boîte stérile peut être affectée à une intervention.");
        Status = InstrumentSetStatus.InUse;
        Touch();
        return Result.Success();
    }

    /// <summary>Retour après utilisation : la boîte devient souillée.</summary>
    public void MarkSoiled()
    {
        Status = InstrumentSetStatus.Soiled;
        Touch();
    }

    /// <summary>Annulation d'une affectation non consommée : retour à l'état stérile.</summary>
    public void RestoreSterile()
    {
        Status = InstrumentSetStatus.Sterile;
        Touch();
    }

    public Result Retire()
    {
        if (Status is InstrumentSetStatus.InUse or InstrumentSetStatus.InSterilization)
            return Result.Failure("Impossible de réformer une boîte en cours d'utilisation ou de stérilisation.");
        Status = InstrumentSetStatus.Retired;
        Touch();
        return Result.Success();
    }

    public static InstrumentSet Rehydrate(
        Guid id, Guid facilityId, string code, string name, string? description, string? category, int itemCount,
        InstrumentSetStatus status, Guid? lastCycleId, DateTimeOffset? sterilizedOnUtc, DateOnly? expiryOn,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, code, name, description, category, itemCount, status, lastCycleId, sterilizedOnUtc, expiryOn, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
