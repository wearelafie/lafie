using Lafie.Organization.Application.Floors;
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
            Guid buildingId, string? search, string? status, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new FloorFilter(search, status, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetFloorsQuery(buildingId, filter), ct));
        });

        byBuilding.MapPost("", async (Guid buildingId, CreateFloorRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateFloorCommand(buildingId, r.Code, r.Name, r.Level, r.DisplayOrder, r.Description), ct);
            return result.IsSuccess
                ? Results.Created($"/floors/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        var byId = endpoints.MapGroup("/floors").WithTags("Floor");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetFloorByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        byId.MapPut("/{id:guid}", async (Guid id, UpdateFloorRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateFloorCommand(id, r.Code, r.Name, r.Level, r.DisplayOrder, r.Description), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        byId.MapPut("/{id:guid}/status", async (Guid id, SetFloorStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetFloorStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        return endpoints;
    }
}
