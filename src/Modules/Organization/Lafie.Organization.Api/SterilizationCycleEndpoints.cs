using Lafie.Core.Context;
using Lafie.Organization.Application.Sterilization.Cycles;
using Lafie.Web.Authorization;
using Lafie.Web.Context;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateSterilizationCycleRequest(string Autoclave, string? CycleNumber, string Program, string? Operator);
public sealed record AddSetToCycleRequest(Guid SetId);
public sealed record CloseSterilizationCycleRequest(bool Passed, decimal? TemperatureC, decimal? PressureBar, int? DurationMin, string? ResultNotes);

/// <summary>Endpoints HTTP des cycles d'autoclave (stérilisation centrale / CSSD).</summary>
public static class SterilizationCycleEndpoints
{
    public static IEndpointRouteBuilder MapSterilizationCycleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var byFacility = endpoints.MapGroup("/facilities/{facilityId:guid}/sterilization/cycles").WithTags("Sterilization");

        byFacility.MapGet("", async (
            Guid facilityId, string? search, string? status, bool? openOnly,
            int? page, int? pageSize, IEstablishmentGuard guard, IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var filter = new SterilizationCycleFilter(search, status, openOnly ?? false, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(new GetSterilizationCyclesQuery(facilityId, filter), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.read");

        byFacility.MapPost("", async (
            Guid facilityId, CreateSterilizationCycleRequest r, IEstablishmentGuard guard, ICurrentUser user,
            IMediator mediator, CancellationToken ct) =>
        {
            guard.EnsureActiveFacility(facilityId);
            var result = await mediator.Send(new CreateSterilizationCycleCommand(
                facilityId, r.Autoclave, r.CycleNumber, r.Program, r.Operator ?? user.Name), ct);
            return result.IsSuccess
                ? Results.Created($"/sterilization/cycles/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        var byId = endpoints.MapGroup("/sterilization/cycles").WithTags("Sterilization")
            .RequireFacilityScope<ISterilizationCycleRepository>("id",
                async (repo, id, ct) => (await repo.GetByIdAsync(id, ct))?.FacilityId);

        byId.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSterilizationCycleQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequirePermission("sterilization.read");

        byId.MapPost("/{id:guid}/load", async (Guid id, AddSetToCycleRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AddSetToCycleCommand(id, r.SetId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        byId.MapDelete("/{id:guid}/load/{setId:guid}", async (Guid id, Guid setId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RemoveSetFromCycleCommand(id, setId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        byId.MapPost("/{id:guid}/start", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new StartSterilizationCycleCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        byId.MapPost("/{id:guid}/close", async (Guid id, CloseSterilizationCycleRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CloseSterilizationCycleCommand(
                id, r.Passed, r.TemperatureC, r.PressureBar, r.DurationMin, r.ResultNotes), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("sterilization.manage");

        return endpoints;
    }
}
