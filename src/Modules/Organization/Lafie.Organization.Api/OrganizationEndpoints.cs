using Lafie.Organization.Application;
using Lafie.Organization.Application.Organizations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record ChangeFacilityStatusRequest(string OperationalStatus);

/// <summary>Endpoints HTTP du module Organization (établissements).</summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/organizations").WithTags("Organization");

        group.MapGet("", async (
            string? search, string? type, string? status, bool? includeArchived, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new OrganizationFilter(search, type, status, includeArchived ?? false, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetOrganizationsQuery(filter), ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetOrganizationByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("", async (OrganizationWriteModel model, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateOrganizationCommand(model), ct);
            return result.IsSuccess
                ? Results.Created($"/organizations/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPut("/{id:guid}", async (Guid id, OrganizationWriteModel model, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateOrganizationCommand(id, model), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        group.MapPut("/{id:guid}/status", async (Guid id, ChangeFacilityStatusRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ChangeOrganizationStatusCommand(id, r.OperationalStatus), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new ArchiveOrganizationCommand(id), ct) is { IsSuccess: true }
                ? Results.NoContent() : Results.BadRequest());

        group.MapPost("/{id:guid}/restore", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new RestoreOrganizationCommand(id), ct) is { IsSuccess: true }
                ? Results.NoContent() : Results.BadRequest());

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteOrganizationCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        });

        return endpoints;
    }
}
