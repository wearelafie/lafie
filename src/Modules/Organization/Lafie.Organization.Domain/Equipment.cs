using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>
/// Agrégat Equipment : actif biomédical / technique rattaché à un établissement
/// (registre des équipements — n° inventaire, garantie, localisation, affectation, état).
/// </summary>
public sealed class Equipment : AggregateRoot<Guid>
{
    public const int InventoryNumberMaxLength = 80;
    public const int SerialNumberMaxLength = 120;
    public const int NameMaxLength = 200;
    public const int CategoryMaxLength = 60;
    public const int BrandMaxLength = 120;
    public const int ModelMaxLength = 120;
    public const int SupplierMaxLength = 200;
    public const int NotesMaxLength = 2000;

    public Guid FacilityId { get; private set; }
    public string InventoryNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public DateOnly? AcquisitionDate { get; private set; }
    public string? Supplier { get; private set; }
    public DateOnly? WarrantyEnd { get; private set; }
    public int? EstimatedLifespanMonths { get; private set; }
    public Guid? SpaceId { get; private set; }
    public Guid? ServiceId { get; private set; }
    public Guid? CareUnitId { get; private set; }
    public EquipmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public DateTimeOffset? ArchivedOnUtc { get; private set; }
    public bool IsArchived => ArchivedOnUtc is not null;

    private Equipment(
        Guid id, Guid facilityId, string inventoryNumber, string? serialNumber, string name, string category,
        string? brand, string? model, DateOnly? acquisitionDate, string? supplier, DateOnly? warrantyEnd,
        int? estimatedLifespanMonths, Guid? spaceId, Guid? serviceId, Guid? careUnitId, EquipmentStatus status,
        string? notes, int displayOrder,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        InventoryNumber = inventoryNumber;
        SerialNumber = serialNumber;
        Name = name;
        Category = category;
        Brand = brand;
        Model = model;
        AcquisitionDate = acquisitionDate;
        Supplier = supplier;
        WarrantyEnd = warrantyEnd;
        EstimatedLifespanMonths = estimatedLifespanMonths;
        SpaceId = spaceId;
        ServiceId = serviceId;
        CareUnitId = careUnitId;
        Status = status;
        Notes = notes;
        DisplayOrder = displayOrder;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
        ArchivedOnUtc = archivedOnUtc;
    }

    public static Result<Equipment> Create(
        Guid facilityId, string inventoryNumber, string? serialNumber, string name, string category,
        string? brand, string? model, DateOnly? acquisitionDate, string? supplier, DateOnly? warrantyEnd,
        int? estimatedLifespanMonths, Guid? spaceId, Guid? serviceId, Guid? careUnitId,
        EquipmentStatus status = EquipmentStatus.Operational, string? notes = null, int displayOrder = 0)
    {
        if (facilityId == Guid.Empty)
            return Result.Failure<Equipment>("L'établissement est obligatoire.");

        var vInv = NormalizeRequired(inventoryNumber, "Le numéro d'inventaire", InventoryNumberMaxLength);
        if (vInv.IsFailure) return Result.Failure<Equipment>(vInv.Error!);
        var vName = NormalizeRequired(name, "Le nom de l'équipement", NameMaxLength);
        if (vName.IsFailure) return Result.Failure<Equipment>(vName.Error!);
        var vCat = NormalizeRequired(category, "La catégorie", CategoryMaxLength);
        if (vCat.IsFailure) return Result.Failure<Equipment>(vCat.Error!);

        var vLifespan = NormalizeLifespan(estimatedLifespanMonths);
        if (vLifespan.IsFailure) return Result.Failure<Equipment>(vLifespan.Error!);

        var e = new Equipment(
            Guid.NewGuid(), facilityId, vInv.Value, Blank(serialNumber, SerialNumberMaxLength), vName.Value, vCat.Value,
            Blank(brand, BrandMaxLength), Blank(model, ModelMaxLength), acquisitionDate, Blank(supplier, SupplierMaxLength),
            warrantyEnd, vLifespan.Value, Nullable(spaceId), Nullable(serviceId), Nullable(careUnitId), status,
            Blank(notes, NotesMaxLength), displayOrder, DateTimeOffset.UtcNow, null, null);

        e.AddDomainEvent(DomainEvent.Create((id, on) =>
            new EquipmentCreated(id, on, e.Id, e.FacilityId, e.Name)));

        return Result.Success(e);
    }

    public static Equipment Rehydrate(
        Guid id, Guid facilityId, string inventoryNumber, string? serialNumber, string name, string category,
        string? brand, string? model, DateOnly? acquisitionDate, string? supplier, DateOnly? warrantyEnd,
        int? estimatedLifespanMonths, Guid? spaceId, Guid? serviceId, Guid? careUnitId, EquipmentStatus status,
        string? notes, int displayOrder,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
        => new(id, facilityId, inventoryNumber, serialNumber, name, category, brand, model, acquisitionDate, supplier,
            warrantyEnd, estimatedLifespanMonths, spaceId, serviceId, careUnitId, status, notes, displayOrder,
            createdOnUtc, updatedOnUtc, archivedOnUtc);

    public Result Update(
        string inventoryNumber, string? serialNumber, string name, string category,
        string? brand, string? model, DateOnly? acquisitionDate, string? supplier, DateOnly? warrantyEnd,
        int? estimatedLifespanMonths, Guid? spaceId, Guid? serviceId, Guid? careUnitId, string? notes, int displayOrder)
    {
        var vInv = NormalizeRequired(inventoryNumber, "Le numéro d'inventaire", InventoryNumberMaxLength);
        if (vInv.IsFailure) return vInv;
        var vName = NormalizeRequired(name, "Le nom de l'équipement", NameMaxLength);
        if (vName.IsFailure) return vName;
        var vCat = NormalizeRequired(category, "La catégorie", CategoryMaxLength);
        if (vCat.IsFailure) return vCat;
        var vLifespan = NormalizeLifespan(estimatedLifespanMonths);
        if (vLifespan.IsFailure) return Result.Failure(vLifespan.Error!);

        InventoryNumber = vInv.Value;
        SerialNumber = Blank(serialNumber, SerialNumberMaxLength);
        Name = vName.Value;
        Category = vCat.Value;
        Brand = Blank(brand, BrandMaxLength);
        Model = Blank(model, ModelMaxLength);
        AcquisitionDate = acquisitionDate;
        Supplier = Blank(supplier, SupplierMaxLength);
        WarrantyEnd = warrantyEnd;
        EstimatedLifespanMonths = vLifespan.Value;
        SpaceId = Nullable(spaceId);
        ServiceId = Nullable(serviceId);
        CareUnitId = Nullable(careUnitId);
        Notes = Blank(notes, NotesMaxLength);
        DisplayOrder = displayOrder;
        Touch();
        return Result.Success();
    }

    public void SetStatus(EquipmentStatus status)
    {
        Status = status;
        Touch();
    }

    public void Archive() { ArchivedOnUtc ??= DateTimeOffset.UtcNow; Touch(); }
    public void Restore() { ArchivedOnUtc = null; Touch(); }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeRequired(string value, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return Result.Failure<string>($"{label} est obligatoire.");
        value = value.Trim();
        return value.Length > maxLength
            ? Result.Failure<string>($"{label} dépasse {maxLength} caractères.")
            : Result.Success(value);
    }

    private static Result<int?> NormalizeLifespan(int? months)
    {
        if (months is null) return Result.Success<int?>(null);
        return months < 0
            ? Result.Failure<int?>("La durée de vie estimée ne peut pas être négative.")
            : Result.Success<int?>(months);
    }

    private static Guid? Nullable(Guid? id) => id is null || id == Guid.Empty ? null : id;

    private static string? Blank(string? s, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > maxLength ? s[..maxLength] : s;
    }
}
