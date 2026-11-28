using Lafie.Core.Context;
using Lafie.Pharmacy.Application.Dispensing;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Pharmacy.Api;

public sealed record PrescriptionLineBody(Guid MedicationId, string? Dosage, int QuantityPrescribed);

public sealed record CreatePrescriptionRequest(
    string PatientName, string? PatientReference, string? PrescriberName, string? Reference,
    DateOnly? PrescribedOn, string? Notes, IReadOnlyList<PrescriptionLineBody> Lines);

public sealed record DispenseLineRequest(int Quantity, string? PerformedBy);

/// <summary>Endpoints HTTP des prescriptions et de la dispensation (module Pharmacie).</summary>
public static class PrescriptionEndpoints
{
    public static IEndpointRouteBuilder MapPrescriptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/pharmacy/prescriptions").WithTags("Pharmacy");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, int? page, int? pageSize,
            IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new PrescriptionFilter(search, status, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetPrescriptionsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreatePrescriptionRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var lines = (r.Lines ?? []).Select(l => new PrescriptionLineInput(l.MedicationId, l.Dosage, l.QuantityPrescribed)).ToList();
            var result = await mediator.Send(new CreatePrescriptionCommand(
                facilityId, r.PatientName, r.PatientReference, r.PrescriberName, r.Reference, r.PrescribedOn, r.Notes, lines), ct);
            return result.IsSuccess
                ? Results.Created($"/pharmacy/prescriptions/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        var byId = endpoints.MapGroup("/pharmacy/prescriptions").WithTags("Pharmacy")
            .RequireFacilityScope<IPrescriptionRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPrescriptionQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byId.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CancelPrescriptionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byId.MapPost("/{id:guid}/lines/{lineId:guid}/dispense", async (
            Guid id, Guid lineId, DispenseLineRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DispensePrescriptionLineCommand(id, lineId, r.Quantity, r.PerformedBy), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.dispense");

        return endpoints;
    }
}
