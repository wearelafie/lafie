using Lafie.Organization.Application.Spaces;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateSpaceRequest(string Code, string Name, string Type, string? Description, decimal? Area, int? MaxCapacity, bool Accessible);
public sealed record UpdateSpaceRequest(string Code, string Name, string Type, string? Description, decimal? Area, int? MaxCapacity, bool Accessible);
public sealed record SetSpaceStatusRequest(string Status);

/// <summary>Endpoints HTTP des espaces.</summary>
public static class SpaceEndpoints
{
    public static IEndpointRouteBuilder MapSpaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFloor = endpoints.MapGroup("/floors/{floorId:guid}/spaces").WithTags("Space");

        byFloor.MapGet("", async (
            Guid floorId, string? search, string? type, string? status, bool? includeArchived, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new SpaceFilter(search, type, status, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetSpacesQuery(floorId, filter), ct));
        });

        byFloor.MapPost("", async (Guid floorId, CreateSpaceRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateSpaceCommand(floorId, r.Code, r.Name, r.Type, r.Description, r.Area, r.MaxCapacity, r.Accessible), ct);
            return result.IsSuccess
                ? Results.Created($"/spaces/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        // Espaces d'un établissement (tous bâtiments/étages) — utilisé par le sélecteur d'association aux unités.
        endpoints.MapGet("/facilities/{facilityId:guid}/spaces", async (
            Guid facilityId, string? search, string? type, string? status, bool? includeArchived, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new SpaceFilter(search, type, status, includeArchived ?? false, page ?? 1, pageSize ?? 50);
            return Results.Ok(await mediator.Send(new GetSpacesByFacilityQuery(facilityId, filter), ct));
        }).WithTags("Space");

        var byId = endpoints.MapGroup("/spaces").WithTags("Space");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetSpaceByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        byId.MapPut("/{id:guid}", async (Guid id, UpdateSpaceRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateSpaceCommand(id, r.Code, r.Name, r.Type, r.Description, r.Area, r.MaxCapacity, r.Accessible), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        byId.MapPut("/{id:guid}/status", async (Guid id, SetSpaceStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetSpaceStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        byId.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveSpaceCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest());
        byId.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreSpaceCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest());
        byId.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteSpaceCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        });

        return endpoints;
    }
}
