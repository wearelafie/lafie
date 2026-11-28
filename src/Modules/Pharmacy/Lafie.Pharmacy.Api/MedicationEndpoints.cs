using Lafie.Core.Context;
using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Domain;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Pharmacy.Api;

public sealed record CreateMedicationRequest(
    string Code, string Name, string? Inn, string Form, string? Strength, string Unit,
    string? AtcCode, string? TherapeuticClass, bool Controlled, string? Notes);

public sealed record UpdateMedicationRequest(
    string Name, string? Inn, string Form, string? Strength, string Unit,
    string? AtcCode, string? TherapeuticClass, bool Controlled, string? Notes);

public sealed record SetMedicationStatusRequest(bool Active);

/// <summary>Endpoints HTTP du catalogue médicaments (module Pharmacie).</summary>
public static class MedicationEndpoints
{
    public static IEndpointRouteBuilder MapMedicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/pharmacy/medications").WithTags("Pharmacy");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? form, string? status, bool? controlled,
            int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new MedicationFilter(search, form, status, controlled, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetMedicationsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateMedicationRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            if (!Enum.TryParse<MedicationForm>(r.Form, out var form))
                return Results.BadRequest(new { error = $"Forme galénique inconnue : « {r.Form} »." });

            var result = await mediator.Send(new CreateMedicationCommand(
                facilityId, r.Code, r.Name, r.Inn, form, r.Strength, r.Unit, r.AtcCode, r.TherapeuticClass, r.Controlled, r.Notes), ct);
            return result.IsSuccess
                ? Results.Created($"/pharmacy/medications/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        var byId = endpoints.MapGroup("/pharmacy/medications").WithTags("Pharmacy")
            .RequireFacilityScope<IMedicationRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMedicationQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("pharmacy.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateMedicationRequest r, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<MedicationForm>(r.Form, out var form))
                return Results.BadRequest(new { error = $"Forme galénique inconnue : « {r.Form} »." });

            var result = await mediator.Send(new UpdateMedicationCommand(
                id, r.Name, r.Inn, form, r.Strength, r.Unit, r.AtcCode, r.TherapeuticClass, r.Controlled, r.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        byId.MapPost("/{id:guid}/status", async (Guid id, SetMedicationStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetMedicationStatusCommand(id, r.Active), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("pharmacy.manage");

        return endpoints;
    }
}
