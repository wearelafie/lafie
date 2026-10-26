using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>Agrégat CareUnit : unité de soins rattachée à un service (et à l'établissement, dénormalisé).</summary>
public sealed class CareUnit : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public Guid FacilityId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public CareUnitType Type { get; private set; }
    public int? BedCapacity { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public CareUnitStatus Status { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public DateTimeOffset? ArchivedOnUtc { get; private set; }
    public bool IsArchived => ArchivedOnUtc is not null;

    private CareUnit(
        Guid id, Guid facilityId, Guid serviceId, string code, string name, CareUnitType type,
        int? bedCapacity, string? description, int displayOrder, CareUnitStatus status,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        ServiceId = serviceId;
        Code = code;
        Name = name;
        Type = type;
        BedCapacity = bedCapacity;
        Description = description;
        DisplayOrder = displayOrder;
        Status = status;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
        ArchivedOnUtc = archivedOnUtc;
    }

    public static Result<CareUnit> Create(
        Guid facilityId, Guid serviceId, string code, string name, CareUnitType type,
        int? bedCapacity = null, string? description = null, int displayOrder = 0)
    {
        if (facilityId == Guid.Empty)
            return Result.Failure<CareUnit>("L'établissement est obligatoire.");
        if (serviceId == Guid.Empty)
            return Result.Failure<CareUnit>("Le service est obligatoire.");
        if (bedCapacity is < 0)
            return Result.Failure<CareUnit>("La capacité en lits ne peut pas être négative.");

        var vName = NormalizeName(name);
        if (vName.IsFailure) return Result.Failure<CareUnit>(vName.Error!);
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure<CareUnit>(vCode.Error!);

        var u = new CareUnit(
            Guid.NewGuid(), facilityId, serviceId, vCode.Value, vName.Value, type,
            bedCapacity, Blank(description), displayOrder,
            CareUnitStatus.Active, DateTimeOffset.UtcNow, null, null);

        u.AddDomainEvent(DomainEvent.Create((id, on) =>
            new CareUnitCreated(id, on, u.Id, u.FacilityId, u.ServiceId, u.Name)));

        return Result.Success(u);
    }

    public static CareUnit Rehydrate(
        Guid id, Guid facilityId, Guid serviceId, string code, string name, CareUnitType type,
        int? bedCapacity, string? description, int displayOrder, CareUnitStatus status,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
        => new(id, facilityId, serviceId, code, name, type, bedCapacity, description, displayOrder, status,
            createdOnUtc, updatedOnUtc, archivedOnUtc);

    public Result Update(
        string code, string name, CareUnitType type, int? bedCapacity, string? description, int displayOrder)
    {
        if (bedCapacity is < 0)
            return Result.Failure("La capacité en lits ne peut pas être négative.");

        var vName = NormalizeName(name);
        if (vName.IsFailure) return vName;
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure(vCode.Error!);

        Code = vCode.Value;
        Name = vName.Value;
        Type = type;
        BedCapacity = bedCapacity;
        Description = Blank(description);
        DisplayOrder = displayOrder;
        Touch();
        return Result.Success();
    }

    public void SetStatus(CareUnitStatus status)
    {
        Status = status;
        Touch();
    }

    public void Archive() { ArchivedOnUtc ??= DateTimeOffset.UtcNow; Touch(); }
    public void Restore() { ArchivedOnUtc = null; Touch(); }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<string>("Le nom de l'unité est obligatoire.");
        name = name.Trim();
        return name.Length > NameMaxLength
            ? Result.Failure<string>($"Le nom dépasse {NameMaxLength} caractères.")
            : Result.Success(name);
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<string>("Le code de l'unité est obligatoire.");
        code = code.Trim();
        return code.Length > CodeMaxLength
            ? Result.Failure<string>($"Le code dépasse {CodeMaxLength} caractères.")
            : Result.Success(code);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
