using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>Agrégat Service : service / département rattaché à un établissement, éventuellement à un service parent.</summary>
public sealed class Service : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;
    public const int HeadMaxLength = 200;

    public Guid FacilityId { get; private set; }
    public Guid? ParentServiceId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public ServiceCategory Category { get; private set; }
    public string? Description { get; private set; }
    public string? Head { get; private set; }
    public int DisplayOrder { get; private set; }
    public ServiceStatus Status { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public DateTimeOffset? ArchivedOnUtc { get; private set; }
    public bool IsArchived => ArchivedOnUtc is not null;

    private Service(
        Guid id, Guid facilityId, Guid? parentServiceId, string code, string name, ServiceCategory category,
        string? description, string? head, int displayOrder, ServiceStatus status,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        ParentServiceId = parentServiceId;
        Code = code;
        Name = name;
        Category = category;
        Description = description;
        Head = head;
        DisplayOrder = displayOrder;
        Status = status;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
        ArchivedOnUtc = archivedOnUtc;
    }

    public static Result<Service> Create(
        Guid facilityId, Guid? parentServiceId, string code, string name, ServiceCategory category,
        string? description = null, string? head = null, int displayOrder = 0)
    {
        if (facilityId == Guid.Empty)
            return Result.Failure<Service>("L'établissement est obligatoire.");

        var vName = NormalizeName(name);
        if (vName.IsFailure) return Result.Failure<Service>(vName.Error!);
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure<Service>(vCode.Error!);

        var s = new Service(
            Guid.NewGuid(), facilityId, parentServiceId == Guid.Empty ? null : parentServiceId,
            vCode.Value, vName.Value, category, Blank(description), Blank(head), displayOrder,
            ServiceStatus.Active, DateTimeOffset.UtcNow, null, null);

        s.AddDomainEvent(DomainEvent.Create((id, on) =>
            new ServiceCreated(id, on, s.Id, s.FacilityId, s.Name)));

        return Result.Success(s);
    }

    public static Service Rehydrate(
        Guid id, Guid facilityId, Guid? parentServiceId, string code, string name, ServiceCategory category,
        string? description, string? head, int displayOrder, ServiceStatus status,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
        => new(id, facilityId, parentServiceId, code, name, category, description, head, displayOrder, status,
            createdOnUtc, updatedOnUtc, archivedOnUtc);

    public Result Update(
        Guid? parentServiceId, string code, string name, ServiceCategory category,
        string? description, string? head, int displayOrder)
    {
        var vName = NormalizeName(name);
        if (vName.IsFailure) return vName;
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure(vCode.Error!);

        if (parentServiceId == Id)
            return Result.Failure("Un service ne peut pas être son propre parent.");

        ParentServiceId = parentServiceId == Guid.Empty ? null : parentServiceId;
        Code = vCode.Value;
        Name = vName.Value;
        Category = category;
        Description = Blank(description);
        Head = Blank(head);
        DisplayOrder = displayOrder;
        Touch();
        return Result.Success();
    }

    public void SetStatus(ServiceStatus status)
    {
        Status = status;
        Touch();
    }

    public void Archive() { ArchivedOnUtc ??= DateTimeOffset.UtcNow; Touch(); }
    public void Restore() { ArchivedOnUtc = null; Touch(); }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<string>("Le nom du service est obligatoire.");
        name = name.Trim();
        return name.Length > NameMaxLength
            ? Result.Failure<string>($"Le nom dépasse {NameMaxLength} caractères.")
            : Result.Success(name);
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<string>("Le code du service est obligatoire.");
        code = code.Trim();
        return code.Length > CodeMaxLength
            ? Result.Failure<string>($"Le code dépasse {CodeMaxLength} caractères.")
            : Result.Success(code);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
