using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>Agrégat Building : bâtiment rattaché à un établissement (Organization).</summary>
public sealed class Building : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public Guid FacilityId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Location { get; private set; }
    public BuildingStatus Status { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private Building(
        Guid id, Guid facilityId, string code, string name, string? description, string? location,
        BuildingStatus status, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Code = code;
        Name = name;
        Description = description;
        Location = location;
        Status = status;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<Building> Create(
        Guid facilityId, string code, string name, string? description = null, string? location = null)
    {
        if (facilityId == Guid.Empty)
            return Result.Failure<Building>("L'établissement est obligatoire.");

        var vName = NormalizeName(name);
        if (vName.IsFailure) return Result.Failure<Building>(vName.Error!);

        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure<Building>(vCode.Error!);

        var b = new Building(
            Guid.NewGuid(), facilityId, vCode.Value, vName.Value,
            Blank(description), Blank(location),
            BuildingStatus.Active, DateTimeOffset.UtcNow, null);

        b.AddDomainEvent(DomainEvent.Create((id, on) =>
            new BuildingCreated(id, on, b.Id, b.FacilityId, b.Name)));

        return Result.Success(b);
    }

    public static Building Rehydrate(
        Guid id, Guid facilityId, string code, string name, string? description, string? location,
        BuildingStatus status, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, code, name, description, location, status, createdOnUtc, updatedOnUtc);

    public Result Update(string code, string name, string? description, string? location)
    {
        var vName = NormalizeName(name);
        if (vName.IsFailure) return vName;
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure(vCode.Error!);

        Code = vCode.Value;
        Name = vName.Value;
        Description = Blank(description);
        Location = Blank(location);
        Touch();
        return Result.Success();
    }

    public void SetStatus(BuildingStatus status)
    {
        Status = status;
        Touch();
    }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<string>("Le nom du bâtiment est obligatoire.");
        name = name.Trim();
        return name.Length > NameMaxLength
            ? Result.Failure<string>($"Le nom dépasse {NameMaxLength} caractères.")
            : Result.Success(name);
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<string>("Le code du bâtiment est obligatoire.");
        code = code.Trim();
        return code.Length > CodeMaxLength
            ? Result.Failure<string>($"Le code dépasse {CodeMaxLength} caractères.")
            : Result.Success(code);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
