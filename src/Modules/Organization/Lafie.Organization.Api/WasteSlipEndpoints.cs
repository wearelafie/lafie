using Lafie.Core.Context;
using Lafie.Organization.Application.Waste.Slips;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateWasteSlipRequest(string Reference, string Stream, string? Carrier, string? Destination, string? Treatment);
public sealed record UpdateWasteSlipRequest(string? Carrier, string? Destination, string? Treatment, string? Notes);
public sealed record AddCollectionToSlipRequest(Guid CollectionId);
public sealed record MarkSlipTreatedRequest(string? Treatment);

/// <summary>Endpoints HTTP des bordereaux d'élimination (DASRI &amp; assimilés).</summary>
public static class WasteSlipEndpoints
{
    public static IEndpointRouteBuilder MapWasteSlipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/waste/slips").WithTags("Waste");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? stream, string? status, bool? openOnly,
            int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new WasteSlipFilter(search, stream, status, openOnly ?? false, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetWasteSlipsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateWasteSlipRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateWasteSlipCommand(
                facilityId, r.Reference, r.Stream, r.Carrier, r.Destination, r.Treatment), ct);
            return result.IsSuccess
                ? Results.Created($"/waste/slips/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        var byId = endpoints.MapGroup("/waste/slips").WithTags("Waste");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWasteSlipQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("waste.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateWasteSlipRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateWasteSlipCommand(id, r.Carrier, r.Destination, r.Treatment, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        byId.MapPost("/{id:guid}/collections", async (Guid id, AddCollectionToSlipRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AddCollectionToSlipCommand(id, r.CollectionId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        byId.MapDelete("/{id:guid}/collections/{collectionId:guid}", async (Guid id, Guid collectionId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RemoveCollectionFromSlipCommand(id, collectionId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        byId.MapPost("/{id:guid}/ship", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ShipWasteSlipCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        byId.MapPost("/{id:guid}/treat", async (Guid id, MarkSlipTreatedRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new MarkSlipTreatedCommand(id, r.Treatment), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        byId.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CancelWasteSlipCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("waste.manage");

        return endpoints;
    }
}
