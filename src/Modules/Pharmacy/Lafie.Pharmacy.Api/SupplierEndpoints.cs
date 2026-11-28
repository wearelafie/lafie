using Lafie.Core.Context;
using Lafie.Pharmacy.Application.Purchasing;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Pharmacy.Api;

public sealed record CreateSupplierRequest(
    string Code, string Name, string? ContactName, string? Email, string? Phone, string? Address, string? Notes);

public sealed record UpdateSupplierRequest(
    string Name, string? ContactName, string? Email, string? Phone, string? Address, string? Notes);

public sealed record SetSupplierStatusRequest(bool Active);

/// <summary>Endpoints HTTP des fournisseurs (module Pharmacie).</summary>
public static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/pharmacy/suppliers").WithTags("Pharmacy");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, bool? active, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new GetSuppliersQuery(facilityId, new SupplierFilter(search, active, page ?? 1, pageSize ?? 20)), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateSupplierRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateSupplierCommand(
                facilityId, r.Code, r.Name, r.ContactName, r.Email, r.Phone, r.Address, r.Notes), ct);
            return result.IsSuccess
                ? Results.Created($"/pharmacy/suppliers/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        var byId = endpoints.MapGroup("/pharmacy/suppliers").WithTags("Pharmacy")
            .RequireFacilityScope<ISupplierRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSupplierQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateSupplierRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateSupplierCommand(id, r.Name, r.ContactName, r.Email, r.Phone, r.Address, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byId.MapPost("/{id:guid}/status", async (Guid id, SetSupplierStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetSupplierStatusCommand(id, r.Active), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        return endpoints;
    }
}
