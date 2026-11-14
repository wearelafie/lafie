using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Organization.Domain;

/// <summary>Intervention de maintenance (préventive / corrective) ou réparation sur un équipement.</summary>
public sealed class EquipmentMaintenance : BaseEntity<Guid>
{
    public const int DescriptionMaxLength = 2000;
    public const int ProviderMaxLength = 200;
    public const int NotesMaxLength = 2000;

    public Guid EquipmentId { get; private set; }
    public MaintenanceType Type { get; private set; }
    public MaintenanceStatus Status { get; private set; }
    public DateOnly? ScheduledOn { get; private set; }
    public DateOnly? PerformedOn { get; private set; }
    public string? Provider { get; private set; }
    public decimal? Cost { get; private set; }
    public string Description { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private EquipmentMaintenance(
        Guid id, Guid equipmentId, MaintenanceType type, MaintenanceStatus status, DateOnly? scheduledOn,
        DateOnly? performedOn, string? provider, decimal? cost, string description, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        EquipmentId = equipmentId;
        Type = type;
        Status = status;
        ScheduledOn = scheduledOn;
        PerformedOn = performedOn;
        Provider = provider;
        Cost = cost;
        Description = description;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<EquipmentMaintenance> Create(
        Guid equipmentId, MaintenanceType type, MaintenanceStatus status, DateOnly? scheduledOn,
        DateOnly? performedOn, string? provider, decimal? cost, string description, string? notes)
    {
        if (equipmentId == Guid.Empty)
            return Result.Failure<EquipmentMaintenance>("L'équipement est obligatoire.");

        var vDesc = NormalizeDescription(description);
        if (vDesc.IsFailure) return Result.Failure<EquipmentMaintenance>(vDesc.Error!);
        var vCost = NormalizeCost(cost);
        if (vCost.IsFailure) return Result.Failure<EquipmentMaintenance>(vCost.Error!);

        return Result.Success(new EquipmentMaintenance(
            Guid.NewGuid(), equipmentId, type, status, scheduledOn, performedOn,
            Blank(provider, ProviderMaxLength), vCost.Value, vDesc.Value, Blank(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null));
    }

    public static EquipmentMaintenance Rehydrate(
        Guid id, Guid equipmentId, MaintenanceType type, MaintenanceStatus status, DateOnly? scheduledOn,
        DateOnly? performedOn, string? provider, decimal? cost, string description, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, equipmentId, type, status, scheduledOn, performedOn, provider, cost, description, notes, createdOnUtc, updatedOnUtc);

    public Result Update(
        MaintenanceType type, DateOnly? scheduledOn, DateOnly? performedOn,
        string? provider, decimal? cost, string description, string? notes)
    {
        var vDesc = NormalizeDescription(description);
        if (vDesc.IsFailure) return vDesc;
        var vCost = NormalizeCost(cost);
        if (vCost.IsFailure) return Result.Failure(vCost.Error!);

        Type = type;
        ScheduledOn = scheduledOn;
        PerformedOn = performedOn;
        Provider = Blank(provider, ProviderMaxLength);
        Cost = vCost.Value;
        Description = vDesc.Value;
        Notes = Blank(notes, NotesMaxLength);
        Touch();
        return Result.Success();
    }

    public void SetStatus(MaintenanceStatus status)
    {
        Status = status;
        // Compléter : marquer la date de réalisation si on passe à « Terminée » sans date.
        if (status == MaintenanceStatus.Completed && PerformedOn is null)
            PerformedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        Touch();
    }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return Result.Failure<string>("La description est obligatoire.");
        description = description.Trim();
        return description.Length > DescriptionMaxLength
            ? Result.Failure<string>($"La description dépasse {DescriptionMaxLength} caractères.")
            : Result.Success(description);
    }

    private static Result<decimal?> NormalizeCost(decimal? cost)
    {
        if (cost is null) return Result.Success<decimal?>(null);
        return cost < 0
            ? Result.Failure<decimal?>("Le coût ne peut pas être négatif.")
            : Result.Success<decimal?>(cost);
    }

    private static string? Blank(string? s, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > maxLength ? s[..maxLength] : s;
    }
}
