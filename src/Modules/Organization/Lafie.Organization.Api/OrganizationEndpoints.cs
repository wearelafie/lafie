using Lafie.Organization.Application.Organizations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

/// <summary>Requête de création d'organisation (corps HTTP).</summary>
public sealed record CreateOrganizationRequest(string Name, string Type, string? Code, Guid? ParentId);

/// <summary>Endpoints HTTP du module Organization (minimal API).</summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/organizations").WithTags("Organization");

        group.MapGet("", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetOrganizationsQuery(), ct)));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetOrganizationByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("", async (CreateOrganizationRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CreateOrganizationCommand(request.Name, request.Type, request.Code, request.ParentId), ct);

            return result.IsSuccess
                ? Results.Created($"/organizations/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        return endpoints;
    }
}
