using Lafie.Organization.Application;
using Lafie.Organization.Application.Organizations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

/// <summary>Requête de création d'organisation.</summary>
public sealed record CreateOrganizationRequest(
    string Name, string Type, string? Code, string? ShortName, string? Description,
    string? Phone, string? Email, string? Timezone, string? City, string? Country, Guid? ParentId);

/// <summary>Requête de mise à jour d'organisation.</summary>
public sealed record UpdateOrganizationRequest(
    string Name, string? ShortName, string? Description,
    string? Phone, string? Email, string? Timezone, string? City, string? Country);

/// <summary>Endpoints HTTP du module Organization (minimal API).</summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/organizations").WithTags("Organization");

        group.MapGet("", async (
            string? search, string? type, string? status, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var filter = new OrganizationFilter(search, type, status, page ?? 1, pageSize ?? 20);
            return Results.Ok(await mediator.Send(new GetOrganizationsQuery(filter), ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetOrganizationByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("", async (CreateOrganizationRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateOrganizationCommand(
                r.Name, r.Type, r.Code, r.ShortName, r.Description, r.Phone, r.Email, r.Timezone, r.City, r.Country, r.ParentId), ct);
            return result.IsSuccess
                ? Results.Created($"/organizations/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateOrganizationRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateOrganizationCommand(
                id, r.Name, r.ShortName, r.Description, r.Phone, r.Email, r.Timezone, r.City, r.Country), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeactivateOrganizationCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ActivateOrganizationCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        return endpoints;
    }
}
