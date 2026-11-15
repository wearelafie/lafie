using Lafie.Core.Context;
using Lafie.Organization.Application.Sterilization.Sets;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateInstrumentSetRequest(string Code, string Name, string? Description, string? Category, int ItemCount);
public sealed record UpdateInstrumentSetRequest(string Name, string? Description, string? Category, int ItemCount);
public sealed record AllocateSetRequest(string InterventionLabel, string? PatientRef, DateOnly? ScheduledOn, string? PerformedBy, string? Notes);
public sealed record TransitionSetUsageRequest(string Action);

/// <summary>Endpoints HTTP des boîtes d'instruments (stérilisation centrale / CSSD).</summary>
public static class InstrumentSetEndpoints
{
    public static IEndpointRouteBuilder MapInstrumentSetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/sterilization/instrument-sets").WithTags("Sterilization");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, string? category,
            int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new InstrumentSetFilter(search, status, category, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetInstrumentSetsQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateInstrumentSetRequest r, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateInstrumentSetCommand(
                facilityId, r.Code, r.Name, r.Description, r.Category, r.ItemCount), ct);
            return result.IsSuccess
                ? Results.Created($"/sterilization/instrument-sets/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        var byId = endpoints.MapGroup("/sterilization/instrument-sets").WithTags("Sterilization");

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInstrumentSetQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("sterilization.read");

        byId.MapPut("/{id:guid}", async (Guid id, UpdateInstrumentSetRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateInstrumentSetCommand(id, r.Name, r.Description, r.Category, r.ItemCount), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        byId.MapPost("/{id:guid}/retire", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RetireInstrumentSetCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        byId.MapPost("/{id:guid}/allocate", async (Guid id, AllocateSetRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AllocateSetCommand(
                id, r.InterventionLabel, r.PatientRef, r.ScheduledOn, r.PerformedBy, r.Notes), ct);
            return result.IsSuccess
                ? Results.Created($"/sterilization/instrument-sets/{id}", new { usageId = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        byId.MapPost("/usages/{usageId:guid}/transition", async (Guid usageId, TransitionSetUsageRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new TransitionSetUsageCommand(usageId, r.Action), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        return endpoints;
    }
}
