using Lafie.Core.Context;
using Lafie.Pharmacy.Application.Purchasing;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Pharmacy.Api;

public sealed record PurchaseOrderLineBody(Guid MedicationId, int QuantityOrdered, decimal? UnitPrice);

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId, string Reference, DateOnly? OrderDate, DateOnly? ExpectedOn, string? Notes,
    IReadOnlyList<PurchaseOrderLineBody> Lines);

public sealed record UpdatePurchaseOrderRequest(
    Guid SupplierId, DateOnly? OrderDate, DateOnly? ExpectedOn, string? Notes, IReadOnlyList<PurchaseOrderLineBody> Lines);

public sealed record ReceiveLineRequest(
    int Quantity, string? LotNumber, DateOnly? ExpiryOn, DateOnly? ReceivedOn, string? PerformedBy);

/// <summary>Endpoints HTTP des bons de commande fournisseurs (module Pharmacie).</summary>
public static class PurchaseOrderEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/pharmacy/purchase-orders").WithTags("Pharmacy");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, Guid? supplierId, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new PurchaseOrderFilter(search, status, supplierId, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetPurchaseOrdersQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreatePurchaseOrderRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var lines = (r.Lines ?? []).Select(l => new PurchaseOrderLineInput(l.MedicationId, l.QuantityOrdered, l.UnitPrice)).ToList();
            var result = await mediator.Send(new CreatePurchaseOrderCommand(
                facilityId, r.SupplierId, r.Reference, r.OrderDate, r.ExpectedOn, r.Notes, lines), ct);
            return result.IsSuccess
                ? Results.Created($"/pharmacy/purchase-orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        var byId = endpoints.MapGroup("/pharmacy/purchase-orders").WithTags("Pharmacy")
            .RequireFacilityScope<IPurchaseOrderRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPurchaseOrderQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdatePurchaseOrderRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var lines = (r.Lines ?? []).Select(l => new PurchaseOrderLineInput(l.MedicationId, l.QuantityOrdered, l.UnitPrice)).ToList();
            var result = await mediator.Send(new UpdatePurchaseOrderCommand(id, r.SupplierId, r.OrderDate, r.ExpectedOn, r.Notes, lines), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byId.MapPost("/{id:guid}/submit", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SubmitPurchaseOrderCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byId.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CancelPurchaseOrderCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byId.MapPost("/{id:guid}/lines/{lineId:guid}/receive", async (
            Guid id, Guid lineId, ReceiveLineRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ReceivePurchaseOrderLineCommand(
                id, lineId, r.Quantity, r.LotNumber, r.ExpiryOn, r.ReceivedOn, r.PerformedBy), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        return endpoints;
    }
}
