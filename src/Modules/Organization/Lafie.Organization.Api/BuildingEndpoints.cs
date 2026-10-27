using Lafie.Core.Context;
using Lafie.Organization.Application.Buildings;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateBuildingRequest(string Code, string Name, string? Description, string? Location);
public sealed record UpdateBuildingRequest(string Code, string Name, string? Description, string? Location);
public sealed record SetBuildingStatusRequest(string Status);

/// <summary>Endpoints HTTP des bâtiments.</summary>
public static class BuildingEndpoints
{
    public static IEndpointRouteBuilder MapBuildingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Liste/création rattachées à un établissement.
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/buildings").WithTags("Building");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, bool? includeArchived, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new BuildingFilter(search, status, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetBuildingsQuery(facilityId, filter), ct));
        }).RequirePermission("infrastructure.read");

        byFacility.MapPost("", async (Guid facilityId, CreateBuildingRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateBuildingCommand(facilityId, r.Code, r.Name, r.Description, r.Location), ct);
            return result.IsSuccess
                ? Results.Created($"/buildings/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        // Accès direct par identifiant de bâtiment.
        var byId = endpoints.MapGroup("/buildings").WithTags("Building");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetBuildingByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).RequirePermission("infrastructure.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateBuildingRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateBuildingCommand(id, r.Code, r.Name, r.Description, r.Location), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPut("/{id:guid}/status", async (Guid id, SetBuildingStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetBuildingStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveBuildingCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreBuildingCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteBuildingCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        return endpoints;
    }
}
