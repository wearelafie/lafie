using Lafie.Organization.Application.Floors;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateFloorRequest(string Code, string Name, int? Level, int DisplayOrder, string? Description);
public sealed record UpdateFloorRequest(string Code, string Name, int? Level, int DisplayOrder, string? Description);
public sealed record SetFloorStatusRequest(string Status);

/// <summary>Endpoints HTTP des étages.</summary>
public static class FloorEndpoints
{
    public static IEndpointRouteBuilder MapFloorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byBuilding = endpoints.MapGroup("/buildings/{buildingId:guid}/floors").WithTags("Floor");

        byBuilding.MapGet("", async (
            Guid buildingId, string? search, string? status, bool? includeArchived, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new FloorFilter(search, status, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetFloorsQuery(buildingId, filter), ct));
        }).RequirePermission("infrastructure.read");

        byBuilding.MapPost("", async (Guid buildingId, CreateFloorRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateFloorCommand(buildingId, r.Code, r.Name, r.Level, r.DisplayOrder, r.Description), ct);
            return result.IsSuccess
                ? Results.Created($"/floors/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        var byId = endpoints.MapGroup("/floors").WithTags("Floor");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetFloorByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).RequirePermission("infrastructure.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateFloorRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateFloorCommand(id, r.Code, r.Name, r.Level, r.DisplayOrder, r.Description), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPut("/{id:guid}/status", async (Guid id, SetFloorStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetFloorStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveFloorCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreFloorCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteFloorCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        return endpoints;
    }
}
