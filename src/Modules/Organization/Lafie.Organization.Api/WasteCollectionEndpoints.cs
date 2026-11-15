using Lafie.Core.Context;
using Lafie.Organization.Application.Waste.Collections;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateWasteCollectionRequest(
    string Stream, decimal WeightKg, int ContainerCount, Guid? SpaceId, DateOnly? CollectedOn, string? CollectedBy, string? Notes);
public sealed record UpdateWasteCollectionRequest(
    string Stream, decimal WeightKg, int ContainerCount, Guid? SpaceId, DateOnly? CollectedOn, string? CollectedBy, string? Notes);

/// <summary>Endpoints HTTP des collectes de déchets (DASRI &amp; assimilés).</summary>
public static class WasteCollectionEndpoints
{
    public static IEndpointRouteBuilder MapWasteCollectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/waste/collections").WithTags("Waste");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? stream, string? status, bool? unassignedOnly,
            int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new WasteCollectionFilter(search, stream, status, unassignedOnly ?? false, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetWasteCollectionsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateWasteCollectionRequest r, IEstablishmentGuard guard, ICurrentUser user,
            IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateWasteCollectionCommand(
                facilityId, r.Stream, r.WeightKg, r.ContainerCount, r.SpaceId, r.CollectedOn,
                string.IsNullOrWhiteSpace(r.CollectedBy) ? user.Name : r.CollectedBy, r.Notes), ct);
            return result.IsSuccess
                ? Results.Created($"/waste/collections/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        var byId = endpoints.MapGroup("/waste/collections").WithTags("Waste");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateWasteCollectionRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateWasteCollectionCommand(
                id, r.Stream, r.WeightKg, r.ContainerCount, r.SpaceId, r.CollectedOn, r.CollectedBy, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        byId.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CancelWasteCollectionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        return endpoints;
    }
}
