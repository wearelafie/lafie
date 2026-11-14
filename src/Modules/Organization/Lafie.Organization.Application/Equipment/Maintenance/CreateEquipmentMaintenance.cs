using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Maintenance;

/// <summary>Enregistre une intervention de maintenance (préventive / corrective) sur un équipement.</summary>
public sealed record CreateEquipmentMaintenanceCommand(
    Guid EquipmentId, string Type, string? Status, DateOnly? ScheduledOn, DateOnly? PerformedOn,
    string? Provider, decimal? Cost, string Description, string? Notes)
    : IRequest<Result<Guid>>;

public sealed class CreateEquipmentMaintenanceHandler(
    IEquipmentMaintenanceRepository maintenances,
    IEquipmentRepository equipment) : IRequestHandler<CreateEquipmentMaintenanceCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateEquipmentMaintenanceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MaintenanceType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure<Guid>($"Type de maintenance inconnu : '{command.Type}'.");

        var status = MaintenanceStatus.Planned;
        if (!string.IsNullOrWhiteSpace(command.Status)
            && !Enum.TryParse(command.Status, ignoreCase: true, out status))
            return Result.Failure<Guid>($"Statut de maintenance inconnu : '{command.Status}'.");

        if (await equipment.GetByIdAsync(command.EquipmentId, cancellationToken) is null)
            return Result.Failure<Guid>("Équipement introuvable.");

        var created = EquipmentMaintenance.Create(
            command.EquipmentId, type, status, command.ScheduledOn, command.PerformedOn,
            command.Provider, command.Cost, command.Description, command.Notes);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await maintenances.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
