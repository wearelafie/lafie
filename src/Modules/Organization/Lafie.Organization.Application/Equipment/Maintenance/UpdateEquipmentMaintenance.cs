using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Maintenance;

/// <summary>Met à jour une intervention de maintenance (hors statut).</summary>
public sealed record UpdateEquipmentMaintenanceCommand(
    Guid Id, string Type, DateOnly? ScheduledOn, DateOnly? PerformedOn,
    string? Provider, decimal? Cost, string Description, string? Notes)
    : IRequest<Result>;

public sealed class UpdateEquipmentMaintenanceHandler(IEquipmentMaintenanceRepository maintenances)
    : IRequestHandler<UpdateEquipmentMaintenanceCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateEquipmentMaintenanceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MaintenanceType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure($"Type de maintenance inconnu : '{command.Type}'.");

        var item = await maintenances.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Intervention {command.Id} introuvable.");

        var updated = item.Update(type, command.ScheduledOn, command.PerformedOn, command.Provider, command.Cost, command.Description, command.Notes);
        if (updated.IsFailure) return updated;

        await maintenances.UpdateAsync(item, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Change le statut d'une intervention de maintenance.</summary>
public sealed record SetMaintenanceStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetMaintenanceStatusHandler(IEquipmentMaintenanceRepository maintenances)
    : IRequestHandler<SetMaintenanceStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetMaintenanceStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MaintenanceStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"Statut inconnu : '{command.Status}'.");

        var item = await maintenances.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Intervention {command.Id} introuvable.");

        item.SetStatus(status);
        await maintenances.UpdateAsync(item, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime une intervention de maintenance.</summary>
public sealed record DeleteEquipmentMaintenanceCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteEquipmentMaintenanceHandler(IEquipmentMaintenanceRepository maintenances)
    : IRequestHandler<DeleteEquipmentMaintenanceCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteEquipmentMaintenanceCommand command, CancellationToken cancellationToken)
    {
        if (await maintenances.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Intervention {command.Id} introuvable.");

        await maintenances.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
