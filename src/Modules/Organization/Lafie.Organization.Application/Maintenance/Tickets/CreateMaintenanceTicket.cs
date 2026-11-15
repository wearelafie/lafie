using Lafie.Core.Common;
using Lafie.Organization.Application.Equipment;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Déclare un incident technique (ticket curatif) sur un équipement ou un espace.</summary>
public sealed record CreateMaintenanceTicketCommand(
    Guid FacilityId, string TargetType, Guid TargetId, string Title, string? Description,
    string Criticality, bool Blocking, string? ReportedBy) : IRequest<Result<Guid>>;

public sealed class CreateMaintenanceTicketHandler(
    IMaintenanceTicketRepository tickets,
    IEquipmentRepository equipment,
    ISpaceRepository spaces) : IRequestHandler<CreateMaintenanceTicketCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateMaintenanceTicketCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TicketTargetType>(command.TargetType, ignoreCase: true, out var targetType))
            return Result.Failure<Guid>($"Type de cible inconnu : '{command.TargetType}'.");
        if (!Enum.TryParse<TicketCriticality>(command.Criticality, ignoreCase: true, out var criticality))
            return Result.Failure<Guid>($"Criticité inconnue : '{command.Criticality}'.");

        // La cible doit exister et appartenir à l'établissement.
        if (targetType is TicketTargetType.Equipment)
        {
            var eq = await equipment.GetByIdAsync(command.TargetId, cancellationToken);
            if (eq is null || eq.FacilityId != command.FacilityId) return Result.Failure<Guid>("Équipement cible introuvable.");
        }
        else
        {
            var sp = await spaces.GetByIdAsync(command.TargetId, cancellationToken);
            if (sp is null || sp.FacilityId != command.FacilityId) return Result.Failure<Guid>("Espace cible introuvable.");
        }

        var created = MaintenanceTicket.Create(
            command.FacilityId, targetType, command.TargetId, command.Title, command.Description,
            criticality, command.Blocking, command.ReportedBy);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        await tickets.AddAsync(created.Value, cancellationToken);

        // Verrouillage automatique : un ticket bloquant sur un espace le bascule en Blocked.
        if (created.Value.IsActivelyBlocking && targetType is TicketTargetType.Space)
        {
            var space = await spaces.FindAsync(command.TargetId, cancellationToken);
            if (space is not null && space.OperationalState != SpaceOperationalState.Blocked)
            {
                space.SetOperationalState(SpaceOperationalState.Blocked);
                await spaces.UpdateAsync(space, cancellationToken);
            }
        }

        return Result.Success(created.Value.Id);
    }
}
