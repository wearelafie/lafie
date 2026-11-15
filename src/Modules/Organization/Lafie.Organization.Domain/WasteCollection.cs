using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>
/// Collecte pesée de déchets à un point de production (DASRI &amp; assimilés). Son <see cref="Status"/>
/// suit le circuit d'élimination : collectée → rattachée à un bordereau → éliminée. Le rattachement
/// et la propagation de l'état du bordereau sont orchestrés par les handlers.
/// </summary>
public sealed class WasteCollection : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public WasteStream Stream { get; private set; }
    public decimal WeightKg { get; private set; }
    public int ContainerCount { get; private set; }
    public WasteCollectionStatus Status { get; private set; }
    public Guid? SpaceId { get; private set; }
    public Guid? SlipId { get; private set; }
    public DateOnly? CollectedOn { get; private set; }
    public string? CollectedBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private WasteCollection(
        Guid id, Guid facilityId, WasteStream stream, decimal weightKg, int containerCount, WasteCollectionStatus status,
        Guid? spaceId, Guid? slipId, DateOnly? collectedOn, string? collectedBy, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Stream = stream;
        WeightKg = weightKg;
        ContainerCount = containerCount;
        Status = status;
        SpaceId = spaceId;
        SlipId = slipId;
        CollectedOn = collectedOn;
        CollectedBy = collectedBy;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<WasteCollection> Create(
        Guid facilityId, WasteStream stream, decimal weightKg, int containerCount,
        Guid? spaceId, DateOnly? collectedOn, string? collectedBy, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<WasteCollection>("Établissement obligatoire.");
        if (weightKg < 0) return Result.Failure<WasteCollection>("Le poids ne peut pas être négatif.");
        if (containerCount < 0) return Result.Failure<WasteCollection>("Le nombre de contenants ne peut pas être négatif.");

        var collection = new WasteCollection(
            Guid.NewGuid(), facilityId, stream, weightKg, containerCount, WasteCollectionStatus.Collected,
            spaceId, null, collectedOn, Trim(collectedBy, NameMaxLength), Trim(notes, TextMaxLength),
            DateTimeOffset.UtcNow, null);
        return Result.Success(collection);
    }

    public Result Edit(WasteStream stream, decimal weightKg, int containerCount, Guid? spaceId, DateOnly? collectedOn, string? collectedBy, string? notes)
    {
        if (Status is not WasteCollectionStatus.Collected)
            return Result.Failure("Seule une collecte non rattachée peut être modifiée.");
        if (weightKg < 0) return Result.Failure("Le poids ne peut pas être négatif.");
        if (containerCount < 0) return Result.Failure("Le nombre de contenants ne peut pas être négatif.");
        Stream = stream;
        WeightKg = weightKg;
        ContainerCount = containerCount;
        SpaceId = spaceId;
        CollectedOn = collectedOn;
        CollectedBy = Trim(collectedBy, NameMaxLength);
        Notes = Trim(notes, TextMaxLength);
        Touch();
        return Result.Success();
    }

    /// <summary>Rattachement à un bordereau d'élimination.</summary>
    public Result AttachToSlip(Guid slipId)
    {
        if (Status is not WasteCollectionStatus.Collected) return Result.Failure("La collecte est déjà rattachée ou clôturée.");
        Status = WasteCollectionStatus.Assigned;
        SlipId = slipId;
        Touch();
        return Result.Success();
    }

    /// <summary>Retrait d'un bordereau (encore au brouillon).</summary>
    public Result DetachFromSlip()
    {
        if (Status is not WasteCollectionStatus.Assigned) return Result.Failure("La collecte n'est pas rattachée.");
        Status = WasteCollectionStatus.Collected;
        SlipId = null;
        Touch();
        return Result.Success();
    }

    /// <summary>Bordereau traité : la collecte est éliminée.</summary>
    public void MarkEliminated()
    {
        Status = WasteCollectionStatus.Eliminated;
        Touch();
    }

    public Result Cancel()
    {
        if (Status is WasteCollectionStatus.Eliminated) return Result.Failure("Une collecte éliminée ne peut pas être annulée.");
        Status = WasteCollectionStatus.Cancelled;
        SlipId = null;
        Touch();
        return Result.Success();
    }

    public static WasteCollection Rehydrate(
        Guid id, Guid facilityId, WasteStream stream, decimal weightKg, int containerCount, WasteCollectionStatus status,
        Guid? spaceId, Guid? slipId, DateOnly? collectedOn, string? collectedBy, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, stream, weightKg, containerCount, status, spaceId, slipId, collectedOn, collectedBy, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Trim(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
