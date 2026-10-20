using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>
/// Agrégat Floor : étage d'un bâtiment. <see cref="Level"/> est optionnel et peut être
/// négatif (sous-sol) ou 0 (rez-de-chaussée) ; les bâtiments sans étage formel restent possibles.
/// </summary>
public sealed class Floor : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public Guid BuildingId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public int? Level { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Description { get; private set; }
    public FloorStatus Status { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private Floor(
        Guid id, Guid buildingId, string code, string name, int? level, int displayOrder, string? description,
        FloorStatus status, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        BuildingId = buildingId;
        Code = code;
        Name = name;
        Level = level;
        DisplayOrder = displayOrder;
        Description = description;
        Status = status;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<Floor> Create(
        Guid buildingId, string code, string name, int? level = null, int displayOrder = 0, string? description = null)
    {
        if (buildingId == Guid.Empty)
            return Result.Failure<Floor>("Le bâtiment est obligatoire.");

        var vName = NormalizeName(name);
        if (vName.IsFailure) return Result.Failure<Floor>(vName.Error!);
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure<Floor>(vCode.Error!);

        var f = new Floor(
            Guid.NewGuid(), buildingId, vCode.Value, vName.Value, level, displayOrder,
            Blank(description), FloorStatus.Active, DateTimeOffset.UtcNow, null);

        f.AddDomainEvent(DomainEvent.Create((id, on) => new FloorCreated(id, on, f.Id, f.BuildingId, f.Name)));
        return Result.Success(f);
    }

    public static Floor Rehydrate(
        Guid id, Guid buildingId, string code, string name, int? level, int displayOrder, string? description,
        FloorStatus status, DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, buildingId, code, name, level, displayOrder, description, status, createdOnUtc, updatedOnUtc);

    public Result Update(string code, string name, int? level, int displayOrder, string? description)
    {
        var vName = NormalizeName(name);
        if (vName.IsFailure) return vName;
        var vCode = NormalizeCode(code);
        if (vCode.IsFailure) return Result.Failure(vCode.Error!);

        Code = vCode.Value;
        Name = vName.Value;
        Level = level;
        DisplayOrder = displayOrder;
        Description = Blank(description);
        Touch();
        return Result.Success();
    }

    public void SetStatus(FloorStatus status)
    {
        Status = status;
        Touch();
    }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<string>("Le nom de l'étage est obligatoire.");
        name = name.Trim();
        return name.Length > NameMaxLength
            ? Result.Failure<string>($"Le nom dépasse {NameMaxLength} caractères.")
            : Result.Success(name);
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<string>("Le code de l'étage est obligatoire.");
        code = code.Trim();
        return code.Length > CodeMaxLength
            ? Result.Failure<string>($"Le code dépasse {CodeMaxLength} caractères.")
            : Result.Success(code);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
