using Lafie.Core.Context;
using Lafie.Organization.Application.Maintenance.Tickets;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateMaintenanceTicketRequest(
    string TargetType, Guid TargetId, string Title, string? Description, string Criticality, bool Blocking);
public sealed record UpdateMaintenanceTicketRequest(
    string Title, string? Description, string Criticality, bool Blocking);
public sealed record AssignMaintenanceTicketRequest(string AssigneeType, string AssigneeRef);
public sealed record TransitionMaintenanceTicketRequest(string Action, string? Resolution);
public sealed record AddTicketPartRequest(string Label, string? Reference, int Quantity, decimal? UnitCost);

/// <summary>Endpoints HTTP de la GMAO (tickets de maintenance curative).</summary>
public static class MaintenanceTicketEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/maintenance/tickets").WithTags("Maintenance");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, string? criticality, string? targetType, Guid? targetId,
            bool? openOnly, int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new MaintenanceTicketFilter(search, status, criticality, targetType, targetId, openOnly ?? false, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetMaintenanceTicketsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateMaintenanceTicketRequest r, IEstablishmentGuard guard, ICurrentUser user,
            IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateMaintenanceTicketCommand(
                facilityId, r.TargetType, r.TargetId, r.Title, r.Description, r.Criticality, r.Blocking, user.Name), ct);
            return result.IsSuccess
                ? Results.Created($"/maintenance/tickets/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.ticket.create");

        var byId = endpoints.MapGroup("/maintenance/tickets").WithTags("Maintenance")
            .RequireFacilityScope<IMaintenanceTicketRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMaintenanceTicketQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("maintenance.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateMaintenanceTicketRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateMaintenanceTicketCommand(id, r.Title, r.Description, r.Criticality, r.Blocking), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.manage");

        byId.MapPost("/{id:guid}/assign", async (Guid id, AssignMaintenanceTicketRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AssignMaintenanceTicketCommand(id, r.AssigneeType, r.AssigneeRef), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.manage");

        byId.MapPost("/{id:guid}/transition", async (Guid id, TransitionMaintenanceTicketRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new TransitionMaintenanceTicketCommand(id, r.Action, r.Resolution), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.manage");

        byId.MapPost("/{id:guid}/parts", async (Guid id, AddTicketPartRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AddTicketPartCommand(id, r.Label, r.Reference, r.Quantity, r.UnitCost), ct);
            return result.IsSuccess
                ? Results.Created($"/maintenance/tickets/{id}/parts/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.manage");

        byId.MapDelete("/parts/{partId:guid}", async (Guid partId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteTicketPartCommand(partId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("maintenance.manage");

        return endpoints;
    }
}
