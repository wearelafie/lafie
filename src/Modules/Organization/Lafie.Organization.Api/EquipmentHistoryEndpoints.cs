using Lafie.Organization.Application.Equipment;
using Lafie.Organization.Application.Equipment.Maintenance;
using Lafie.Organization.Application.Equipment.Movements;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Organization.Api;

public sealed record CreateMovementRequest(DateOnly MovedOn, Guid? FromSpaceId, Guid? ToSpaceId, string? Reason, string? PerformedBy, string? Notes, bool? UpdateCurrentLocation);

public sealed record CreateMaintenanceRequest(string Type, string? Status, DateOnly? ScheduledOn, DateOnly? PerformedOn, string? Provider, decimal? Cost, string Description, string? Notes);
public sealed record UpdateMaintenanceRequest(string Type, DateOnly? ScheduledOn, DateOnly? PerformedOn, string? Provider, decimal? Cost, string Description, string? Notes);
public sealed record SetMaintenanceStatusRequest(string Status);

/// <summary>Endpoints HTTP de l'historique des équipements : déplacements et maintenance.</summary>
public static class EquipmentHistoryEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // --- Déplacements ---
        var mv = endpoints.MapGroup("/equipment/{equipmentId:guid}/movements").WithTags("Equipment")
            .RequireFacilityScope<IEquipmentRepository>("equipmentId",
                async (repo, equipmentId, ct) => (await repo.GetByIdAsync(equipmentId, ct))?.FacilityId);

        mv.MapGet("", async (Guid equipmentId, int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetEquipmentMovementsQuery(equipmentId, new EquipmentMovementFilter(page ?? 1, pageSize ?? 20)), ct)))
            .RequirePermission("infrastructure.read");

        mv.MapPost("", async (Guid equipmentId, CreateMovementRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateEquipmentMovementCommand(
                equipmentId, r.MovedOn, r.FromSpaceId, r.ToSpaceId, r.Reason, r.PerformedBy, r.Notes, r.UpdateCurrentLocation ?? true), ct);
            return result.IsSuccess
                ? Results.Created($"/equipment/movements/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        var mvById = endpoints.MapGroup("/equipment/movements").WithTags("Equipment")
            .RequireFacilityScope("id", FacilityOfMovement);

        mvById.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteEquipmentMovementCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        // --- Maintenance ---
        var mt = endpoints.MapGroup("/equipment/{equipmentId:guid}/maintenance").WithTags("Equipment")
            .RequireFacilityScope<IEquipmentRepository>("equipmentId",
                async (repo, equipmentId, ct) => (await repo.GetByIdAsync(equipmentId, ct))?.FacilityId);

        mt.MapGet("", async (Guid equipmentId, string? type, string? status, int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetEquipmentMaintenanceQuery(equipmentId, new EquipmentMaintenanceFilter(type, status, page ?? 1, pageSize ?? 20)), ct)))
            .RequirePermission("infrastructure.read");

        mt.MapPost("", async (Guid equipmentId, CreateMaintenanceRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateEquipmentMaintenanceCommand(
                equipmentId, r.Type, r.Status, r.ScheduledOn, r.PerformedOn, r.Provider, r.Cost, r.Description, r.Notes), ct);
            return result.IsSuccess
                ? Results.Created($"/equipment/maintenance/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        var mtById = endpoints.MapGroup("/equipment/maintenance").WithTags("Equipment")
            .RequireFacilityScope("id", FacilityOfMaintenance);

        mtById.MapPut("/{id:guid}", async (Guid id, UpdateMaintenanceRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateEquipmentMaintenanceCommand(id, r.Type, r.ScheduledOn, r.PerformedOn, r.Provider, r.Cost, r.Description, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        mtById.MapPut("/{id:guid}/status", async (Guid id, SetMaintenanceStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetMaintenanceStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        mtById.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteEquipmentMaintenanceCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        return endpoints;
    }

    /// <summary>Établissement d'une intervention (intervention → équipement → établissement).</summary>
    private static async ValueTask<Guid?> FacilityOfMaintenance(Guid maintenanceId, IServiceProvider sp, CancellationToken ct)
    {
        var maintenance = await sp.GetRequiredService<IEquipmentMaintenanceRepository>().GetByIdAsync(maintenanceId, ct);
        if (maintenance is null) return null;
        return (await sp.GetRequiredService<IEquipmentRepository>().GetByIdAsync(maintenance.EquipmentId, ct))?.FacilityId;
    }

    /// <summary>Établissement d'un déplacement (déplacement → équipement → établissement).</summary>
    private static async ValueTask<Guid?> FacilityOfMovement(Guid movementId, IServiceProvider sp, CancellationToken ct)
    {
        var movement = await sp.GetRequiredService<IEquipmentMovementRepository>().GetByIdAsync(movementId, ct);
        if (movement is null) return null;
        return (await sp.GetRequiredService<IEquipmentRepository>().GetByIdAsync(movement.EquipmentId, ct))?.FacilityId;
    }
}
