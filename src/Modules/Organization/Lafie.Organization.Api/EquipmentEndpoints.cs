using Lafie.Core.Context;
using Lafie.Organization.Application.Equipment;
using Lafie.Organization.Application.Equipment.Dashboard;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateEquipmentRequest(
    string InventoryNumber, string? SerialNumber, string Name, string Category,
    string? Brand, string? Model, DateOnly? AcquisitionDate, string? Supplier, DateOnly? WarrantyEnd,
    int? EstimatedLifespanMonths, Guid? SpaceId, Guid? ServiceId, Guid? CareUnitId,
    string? Status, string? Notes, int DisplayOrder);

public sealed record UpdateEquipmentRequest(
    string InventoryNumber, string? SerialNumber, string Name, string Category,
    string? Brand, string? Model, DateOnly? AcquisitionDate, string? Supplier, DateOnly? WarrantyEnd,
    int? EstimatedLifespanMonths, Guid? SpaceId, Guid? ServiceId, Guid? CareUnitId,
    string? Notes, int DisplayOrder);

public sealed record SetEquipmentStatusRequest(string Status);

/// <summary>Endpoints HTTP des équipements (registre des actifs).</summary>
public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/equipment").WithTags("Equipment");

        byFacility.MapGet("/dashboard", async (
            Guid facilityId, int? warrantyWindow, int? maintenanceWindow,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var options = new EquipmentDashboardOptions(warrantyWindow ?? 60, maintenanceWindow ?? 30);
            return Results.Ok(await mediator.Send(new GetEquipmentDashboardQuery(facilityId, options), ct));
        }).RequirePermission("infrastructure.read");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? category, string? status, Guid? spaceId, Guid? serviceId,
            bool? includeArchived, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new EquipmentFilter(search, category, status, spaceId, serviceId, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetEquipmentQuery(facilityId, filter), ct));
        }).RequirePermission("infrastructure.read");

        byFacility.MapPost("", async (Guid facilityId, CreateEquipmentRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateEquipmentCommand(
                facilityId, r.InventoryNumber, r.SerialNumber, r.Name, r.Category, r.Brand, r.Model,
                r.AcquisitionDate, r.Supplier, r.WarrantyEnd, r.EstimatedLifespanMonths,
                r.SpaceId, r.ServiceId, r.CareUnitId, r.Status, r.Notes, r.DisplayOrder), ct);
            return result.IsSuccess
                ? Results.Created($"/equipment/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        var byId = endpoints.MapGroup("/equipment").WithTags("Equipment");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetEquipmentByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).RequirePermission("infrastructure.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateEquipmentRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateEquipmentCommand(
                id, r.InventoryNumber, r.SerialNumber, r.Name, r.Category, r.Brand, r.Model,
                r.AcquisitionDate, r.Supplier, r.WarrantyEnd, r.EstimatedLifespanMonths,
                r.SpaceId, r.ServiceId, r.CareUnitId, r.Notes, r.DisplayOrder), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPut("/{id:guid}/status", async (Guid id, SetEquipmentStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetEquipmentStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveEquipmentCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreEquipmentCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteEquipmentCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        return endpoints;
    }
}
