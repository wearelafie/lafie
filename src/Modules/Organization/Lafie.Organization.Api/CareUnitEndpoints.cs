using Lafie.Organization.Application.CareUnits;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateCareUnitRequest(string Code, string Name, string Type, int? BedCapacity, string? Description, int DisplayOrder);
public sealed record UpdateCareUnitRequest(string Code, string Name, string Type, int? BedCapacity, string? Description, int DisplayOrder);
public sealed record SetCareUnitStatusRequest(string Status);
public sealed record AssignSpaceRequest(Guid SpaceId);

/// <summary>Endpoints HTTP des unités de soins (et de leur association aux espaces).</summary>
public static class CareUnitEndpoints
{
    public static IEndpointRouteBuilder MapCareUnitEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byService = endpoints.MapGroup("/services/{serviceId:guid}/care-units").WithTags("CareUnit");

        byService.MapGet("", async (
            Guid serviceId, string? search, string? type, string? status, bool? includeArchived, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new CareUnitFilter(search, type, status, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetCareUnitsQuery(serviceId, filter), ct));
        }).RequirePermission("infrastructure.read");

        byService.MapPost("", async (Guid serviceId, CreateCareUnitRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateCareUnitCommand(serviceId, r.Code, r.Name, r.Type, r.BedCapacity, r.Description, r.DisplayOrder), ct);
            return result.IsSuccess
                ? Results.Created($"/care-units/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        var byId = endpoints.MapGroup("/care-units").WithTags("CareUnit");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetCareUnitByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).RequirePermission("infrastructure.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateCareUnitRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateCareUnitCommand(id, r.Code, r.Name, r.Type, r.BedCapacity, r.Description, r.DisplayOrder), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPut("/{id:guid}/status", async (Guid id, SetCareUnitStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetCareUnitStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveCareUnitCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreCareUnitCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCareUnitCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        // --- Association aux espaces (M:N) ---
        byId.MapGet("/{id:guid}/spaces", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetCareUnitSpacesQuery(id), ct))).RequirePermission("infrastructure.read");

        byId.MapPost("/{id:guid}/spaces", async (Guid id, AssignSpaceRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AssignSpaceToCareUnitCommand(id, r.SpaceId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapDelete("/{id:guid}/spaces/{spaceId:guid}", async (Guid id, Guid spaceId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UnassignSpaceFromCareUnitCommand(id, spaceId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        return endpoints;
    }
}
