using Lafie.Core.Context;
using Lafie.Pharmacy.Application.Stock;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Pharmacy.Api;

public sealed record ReceiveStockRequest(
    Guid MedicationId, string? LotNumber, DateOnly? ExpiryOn, int Quantity,
    decimal? UnitCost, string? SupplierName, DateOnly? ReceivedOn, string? Notes, string? PerformedBy);

public sealed record IssueStockRequest(
    Guid MedicationId, int Quantity, string? Reason, string? PerformedBy, string? Notes);

public sealed record SetThresholdRequest(int Threshold, string? Notes);

public sealed record AdjustLotRequest(int NewQuantityOnHand, string? Reason, string? PerformedBy);

/// <summary>Endpoints HTTP du stock pharmaceutique : positions, lots, entrées/sorties (module Pharmacie).</summary>
public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/pharmacy/stock").WithTags("Pharmacy");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, bool? low, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new StockFilter(search, low ?? false, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetStockItemsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byFacility.MapGet("/medications/{medicationId:guid}", async (
            Guid facilityId, Guid medicationId, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new GetMedicationStockQuery(facilityId, medicationId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byFacility.MapPost("/receipts", async (
            Guid facilityId, ReceiveStockRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new ReceiveStockCommand(
                facilityId, r.MedicationId, r.LotNumber, r.ExpiryOn, r.Quantity,
                r.UnitCost, r.SupplierName, r.ReceivedOn, r.Notes, r.PerformedBy), ct);
            return result.IsSuccess
                ? Results.Created($"/pharmacy/stock/lots/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byFacility.MapPost("/issues", async (
            Guid facilityId, IssueStockRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new IssueStockCommand(
                facilityId, r.MedicationId, r.Quantity, r.Reason, r.PerformedBy, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byFacility.MapPut("/medications/{medicationId:guid}/threshold", async (
            Guid facilityId, Guid medicationId, SetThresholdRequest r,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new SetReorderThresholdCommand(facilityId, medicationId, r.Threshold, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        var byLot = endpoints.MapGroup("/pharmacy/stock/lots").WithTags("Pharmacy")
            .RequireFacilityScope<IStockRepository>("id",
                async (repo, id, ct) => (await repo.GetLotByIdAsync(id, ct))?.FacilityId);

        byLot.MapPost("/{id:guid}/adjust", async (Guid id, AdjustLotRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AdjustLotCommand(id, r.NewQuantityOnHand, r.Reason, r.PerformedBy), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        return endpoints;
    }
}
