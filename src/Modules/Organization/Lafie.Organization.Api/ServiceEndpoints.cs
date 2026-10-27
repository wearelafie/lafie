using Lafie.Core.Context;
using Lafie.Organization.Application.Services;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateServiceRequest(Guid? ParentServiceId, string Code, string Name, string Category, string? Description, string? Head, int DisplayOrder);
public sealed record UpdateServiceRequest(Guid? ParentServiceId, string Code, string Name, string Category, string? Description, string? Head, int DisplayOrder);
public sealed record SetServiceStatusRequest(string Status);

/// <summary>Endpoints HTTP des services.</summary>
public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/services").WithTags("Service");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? category, string? status, bool? includeArchived, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new ServiceFilter(search, category, status, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetServicesQuery(facilityId, filter), ct));
        }).RequirePermission("infrastructure.read");

        byFacility.MapPost("", async (Guid facilityId, CreateServiceRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateServiceCommand(facilityId, r.ParentServiceId, r.Code, r.Name, r.Category, r.Description, r.Head, r.DisplayOrder), ct);
            return result.IsSuccess
                ? Results.Created($"/services/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        var byId = endpoints.MapGroup("/services").WithTags("Service");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetServiceByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).RequirePermission("infrastructure.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateServiceRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateServiceCommand(id, r.ParentServiceId, r.Code, r.Name, r.Category, r.Description, r.Head, r.DisplayOrder), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPut("/{id:guid}/status", async (Guid id, SetServiceStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetServiceStatusCommand(id, r.Status), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        byId.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveServiceCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreServiceCommand(id), ct) is { IsSuccess: true } ? Results.NoContent() : Results.BadRequest()).RequirePermission("infrastructure.manage");
        byId.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteServiceCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        }).RequirePermission("infrastructure.manage");

        return endpoints;
    }
}
