using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>Entrée du journal des déplacements d'un équipement (d'un espace à un autre).</summary>
public sealed class EquipmentMovement : BaseEntity<Guid>
{
    public const int ReasonMaxLength = 500;
    public const int PerformedByMaxLength = 200;
    public const int NotesMaxLength = 2000;

    public Guid EquipmentId { get; private set; }
    public DateOnly MovedOn { get; private set; }
    public Guid? FromSpaceId { get; private set; }
    public Guid? ToSpaceId { get; private set; }
    public string? Reason { get; private set; }
    public string? PerformedBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    private EquipmentMovement(
        Guid id, Guid equipmentId, DateOnly movedOn, Guid? fromSpaceId, Guid? toSpaceId,
        string? reason, string? performedBy, string? notes, DateTimeOffset createdOnUtc)
    {
        Id = id;
        EquipmentId = equipmentId;
        MovedOn = movedOn;
        FromSpaceId = fromSpaceId;
        ToSpaceId = toSpaceId;
        Reason = reason;
        PerformedBy = performedBy;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<EquipmentMovement> Create(
        Guid equipmentId, DateOnly movedOn, Guid? fromSpaceId, Guid? toSpaceId,
        string? reason, string? performedBy, string? notes)
    {
        if (equipmentId == Guid.Empty)
            return Result.Failure<EquipmentMovement>("L'équipement est obligatoire.");

        return Result.Success(new EquipmentMovement(
            Guid.NewGuid(), equipmentId, movedOn, Nullable(fromSpaceId), Nullable(toSpaceId),
            Blank(reason, ReasonMaxLength), Blank(performedBy, PerformedByMaxLength), Blank(notes, NotesMaxLength),
            DateTimeOffset.UtcNow));
    }

    public static EquipmentMovement Rehydrate(
        Guid id, Guid equipmentId, DateOnly movedOn, Guid? fromSpaceId, Guid? toSpaceId,
        string? reason, string? performedBy, string? notes, DateTimeOffset createdOnUtc)
        => new(id, equipmentId, movedOn, fromSpaceId, toSpaceId, reason, performedBy, notes, createdOnUtc);

    private static Guid? Nullable(Guid? id) => id is null || id == Guid.Empty ? null : id;

    private static string? Blank(string? s, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > maxLength ? s[..maxLength] : s;
    }
}
