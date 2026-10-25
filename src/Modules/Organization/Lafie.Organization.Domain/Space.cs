using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>
/// Agrégat Space : espace physique rattaché à un étage (et par cohérence à son bâtiment/établissement).
/// Les données structurelles (capacité physique) sont distinctes de l'occupation patient (module futur).
/// </summary>
public sealed class Space : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public Guid FacilityId { get; private set; }
    public Guid BuildingId { get; private set; }
    public Guid FloorId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public SpaceType Type { get; private set; }
    public string? Description { get; private set; }
    public decimal? Area { get; private set; }
    public int? MaxCapacity { get; private set; }
    public bool Accessible { get; private set; }
    public SpaceStatus Status { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public DateTimeOffset? ArchivedOnUtc { get; private set; }
    public bool IsArchived => ArchivedOnUtc is not null;

    private Space(
        Guid id, Guid facilityId, Guid buildingId, Guid floorId, string code, string name, SpaceType type,
        string? description, decimal? area, int? maxCapacity, bool accessible,
        SpaceStatus status, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        BuildingId = buildingId;
        FloorId = floorId;
        Code = code;
        Name = name;
        Type = type;
        Description = description;
        Area = area;
        MaxCapacity = maxCapacity;
        Accessible = accessible;
        Status = status;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
        ArchivedOnUtc = archivedOnUtc;
    }

    public static Result<Space> Create(
        Guid facilityId, Guid buildingId, Guid floorId, string code, string name, SpaceType type,
        string? description = null, decimal? area = null, int? maxCapacity = null, bool accessible = false)
    {
        if (facilityId == Guid.Empty || buildingId == Guid.Empty || floorId == Guid.Empty)
            return Result.Failure<Space>("Établissement, bâtiment et étage sont obligatoires.");

        var vName = NormalizeName(name);
        if (vName.IsFailure) return Result.Failure<Space>(vName.Error!);
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure<Space>(vCode.Error!);
        if (!Enum.IsDefined(type)) return Result.Failure<Space>("Type d'espace invalide.");

        var vMetrics = ValidateMetrics(area, maxCapacity);
        if (vMetrics.IsFailure) return Result.Failure<Space>(vMetrics.Error!);

        var s = new Space(
            Guid.NewGuid(), facilityId, buildingId, floorId, vCode.Value, vName.Value, type,
            Blank(description), area, maxCapacity, accessible,
            SpaceStatus.Active, DateTimeOffset.UtcNow, null, null);

        s.AddDomainEvent(DomainEvent.Create((id, on) => new SpaceCreated(id, on, s.Id, s.FloorId, s.Type, s.Name)));
        return Result.Success(s);
    }

    public static Space Rehydrate(
        Guid id, Guid facilityId, Guid buildingId, Guid floorId, string code, string name, SpaceType type,
        string? description, decimal? area, int? maxCapacity, bool accessible,
        SpaceStatus status, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
        => new(id, facilityId, buildingId, floorId, code, name, type, description, area, maxCapacity, accessible, status, createdOnUtc, updatedOnUtc, archivedOnUtc);

    public Result Update(string code, string name, SpaceType type, string? description, decimal? area, int? maxCapacity, bool accessible)
    {
        var vName = NormalizeName(name);
        if (vName.IsFailure) return vName;
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure(vCode.Error!);
        if (!Enum.IsDefined(type)) return Result.Failure("Type d'espace invalide.");
        var vMetrics = ValidateMetrics(area, maxCapacity);
        if (vMetrics.IsFailure) return vMetrics;

        Code = vCode.Value;
        Name = vName.Value;
        Type = type;
        Description = Blank(description);
        Area = area;
        MaxCapacity = maxCapacity;
        Accessible = accessible;
        Touch();
        return Result.Success();
    }

    public void SetStatus(SpaceStatus status)
    {
        Status = status;
        Touch();
    }

    public void Archive() { ArchivedOnUtc ??= DateTimeOffset.UtcNow; Touch(); }
    public void Restore() { ArchivedOnUtc = null; Touch(); }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result ValidateMetrics(decimal? area, int? maxCapacity)
    {
        if (area is < 0) return Result.Failure("La surface ne peut pas être négative.");
        if (maxCapacity is < 0) return Result.Failure("La capacité ne peut pas être négative.");
        return Result.Success();
    }

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<string>("Le nom de l'espace est obligatoire.");
        name = name.Trim();
        return name.Length > NameMaxLength
            ? Result.Failure<string>($"Le nom dépasse {NameMaxLength} caractères.")
            : Result.Success(name);
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<string>("Le code de l'espace est obligatoire.");
        code = code.Trim();
        return code.Length > CodeMaxLength
            ? Result.Failure<string>($"Le code dépasse {CodeMaxLength} caractères.")
            : Result.Success(code);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
