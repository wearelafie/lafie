using Lafie.Core.Context;
using Lafie.Organization.Application.Hygiene.Cleaning;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateCleaningTaskRequest(Guid SpaceId, string Protocol, string Trigger, string? Notes);
public sealed record AssignCleaningTaskRequest(string AssignedTo);
public sealed record TransitionCleaningTaskRequest(string Action);
public sealed record ToggleCleaningItemRequest(bool Done);
public sealed record AddCleaningItemRequest(string Label);

/// <summary>Endpoints HTTP de l'hygiène hospitalière (bio-nettoyage).</summary>
public static class CleaningTaskEndpoints
{
    public static IEndpointRouteBuilder MapCleaningTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/hygiene/cleaning-tasks").WithTags("Hygiene");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, string? protocol, Guid? spaceId, bool? activeOnly,
            int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new CleaningTaskFilter(search, status, protocol, spaceId, activeOnly ?? false, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetCleaningTasksQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateCleaningTaskRequest r, IEstablishmentGuard guard, ICurrentUser user,
            IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateCleaningTaskCommand(
                facilityId, r.SpaceId, r.Protocol, r.Trigger, user.Name, r.Notes), ct);
            return result.IsSuccess
                ? Results.Created($"/hygiene/cleaning-tasks/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.manage");

        var byId = endpoints.MapGroup("/hygiene/cleaning-tasks").WithTags("Hygiene")
            .RequireFacilityScope<ICleaningTaskRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCleaningTaskQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("hygiene.read");

        byId.MapPost("/{id:guid}/assign", async (Guid id, AssignCleaningTaskRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AssignCleaningTaskCommand(id, r.AssignedTo), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.manage");

        byId.MapPost("/{id:guid}/transition", async (Guid id, TransitionCleaningTaskRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new TransitionCleaningTaskCommand(id, r.Action), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.manage");

        byId.MapPost("/{id:guid}/items", async (Guid id, AddCleaningItemRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AddCleaningItemCommand(id, r.Label), ct);
            return result.IsSuccess
                ? Results.Created($"/hygiene/cleaning-tasks/{id}/items/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.manage");

        byId.MapPut("/items/{itemId:guid}", async (Guid itemId, ToggleCleaningItemRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ToggleCleaningItemCommand(itemId, r.Done), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.manage");

        byId.MapDelete("/items/{itemId:guid}", async (Guid itemId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCleaningItemCommand(itemId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("hygiene.manage");

        return endpoints;
    }
}
