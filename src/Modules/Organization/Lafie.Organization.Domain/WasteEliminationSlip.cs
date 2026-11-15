using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Bordereau d'élimination (type BSD) : regroupe des collectes d'une même filière pour un enlèvement
/// vers un site de traitement. Trace l'expédition et le traitement. La propagation de l'état aux
/// collectes rattachées est orchestrée par les handlers.
/// </summary>
public sealed class WasteEliminationSlip : AggregateRoot<Guid>
{
    public const int RefMaxLength = 100;
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public string Reference { get; private set; }
    public WasteStream Stream { get; private set; }
    public WasteSlipStatus Status { get; private set; }
    public string? Carrier { get; private set; }
    public string? Destination { get; private set; }
    public WasteTreatment? Treatment { get; private set; }
    public decimal TotalWeightKg { get; private set; }
    public DateTimeOffset? ShippedOnUtc { get; private set; }
    public DateTimeOffset? TreatedOnUtc { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private WasteEliminationSlip(
        Guid id, Guid facilityId, string reference, WasteStream stream, WasteSlipStatus status, string? carrier,
        string? destination, WasteTreatment? treatment, decimal totalWeightKg, DateTimeOffset? shippedOnUtc,
        DateTimeOffset? treatedOnUtc, string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Reference = reference;
        Stream = stream;
        Status = status;
        Carrier = carrier;
        Destination = destination;
        Treatment = treatment;
        TotalWeightKg = totalWeightKg;
        ShippedOnUtc = shippedOnUtc;
        TreatedOnUtc = treatedOnUtc;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<WasteEliminationSlip> Create(
        Guid facilityId, string reference, WasteStream stream, string? carrier, string? destination, WasteTreatment? treatment)
    {
        if (facilityId == Guid.Empty) return Result.Failure<WasteEliminationSlip>("Établissement obligatoire.");
        if (string.IsNullOrWhiteSpace(reference)) return Result.Failure<WasteEliminationSlip>("La référence du bordereau est obligatoire.");
        reference = reference.Trim();
        if (reference.Length > RefMaxLength) return Result.Failure<WasteEliminationSlip>($"La référence dépasse {RefMaxLength} caractères.");

        var slip = new WasteEliminationSlip(
            Guid.NewGuid(), facilityId, reference, stream, WasteSlipStatus.Draft, Trim(carrier, NameMaxLength),
            Trim(destination, NameMaxLength), treatment, 0m, null, null, null, DateTimeOffset.UtcNow, null);
        return Result.Success(slip);
    }

    public bool IsDraft => Status is WasteSlipStatus.Draft;

    public Result Edit(string? carrier, string? destination, WasteTreatment? treatment, string? notes)
    {
        if (Status is WasteSlipStatus.Treated or WasteSlipStatus.Cancelled)
            return Result.Failure("Un bordereau traité ou annulé ne peut pas être modifié.");
        Carrier = Trim(carrier, NameMaxLength);
        Destination = Trim(destination, NameMaxLength);
        Treatment = treatment;
        Notes = Trim(notes, TextMaxLength);
        Touch();
        return Result.Success();
    }

    /// <summary>Expédition : fige le poids total cumulé des collectes rattachées.</summary>
    public Result Ship(decimal totalWeightKg)
    {
        if (Status is not WasteSlipStatus.Draft) return Result.Failure("Seul un bordereau au brouillon peut être expédié.");
        Status = WasteSlipStatus.Shipped;
        TotalWeightKg = totalWeightKg < 0 ? 0 : totalWeightKg;
        ShippedOnUtc = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result MarkTreated(WasteTreatment? treatment)
    {
        if (Status is not WasteSlipStatus.Shipped) return Result.Failure("Seul un bordereau expédié peut être marqué traité.");
        Status = WasteSlipStatus.Treated;
        if (treatment is not null) Treatment = treatment;
        TreatedOnUtc = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is WasteSlipStatus.Treated) return Result.Failure("Un bordereau traité ne peut pas être annulé.");
        Status = WasteSlipStatus.Cancelled;
        Touch();
        return Result.Success();
    }

    public static WasteEliminationSlip Rehydrate(
        Guid id, Guid facilityId, string reference, WasteStream stream, WasteSlipStatus status, string? carrier,
        string? destination, WasteTreatment? treatment, decimal totalWeightKg, DateTimeOffset? shippedOnUtc,
        DateTimeOffset? treatedOnUtc, string? notes, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, reference, stream, status, carrier, destination, treatment, totalWeightKg, shippedOnUtc, treatedOnUtc, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
