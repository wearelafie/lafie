using Lafie.Organization.Application.Equipment.Maintenance;
using Lafie.Organization.Application.Equipment.Movements;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
        var mv = endpoints.MapGroup("/equipment/{equipmentId:guid}/movements").WithTags("Equipment");

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

        endpoints.MapDelete("/equipment/movements/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteEquipmentMovementCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).WithTags("Equipment").RequirePermission("infrastructure.manage");

        // --- Maintenance ---
        var mt = endpoints.MapGroup("/equipment/{equipmentId:guid}/maintenance").WithTags("Equipment");

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

        var mtById = endpoints.MapGroup("/equipment/maintenance").WithTags("Equipment");

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
}
