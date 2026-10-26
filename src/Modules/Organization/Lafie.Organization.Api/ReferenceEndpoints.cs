using Lafie.Organization.Application.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

/// <summary>Endpoints HTTP des données de référence (listes de codes + géographie).</summary>
public static class ReferenceEndpoints
{
    public static IEndpointRouteBuilder MapReferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/reference-values/{category}", async (string category, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetReferenceValuesQuery(category), ct))).WithTags("Reference");

        endpoints.MapGet("/geo-divisions", async (string level, string? parentCode, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetGeoDivisionsQuery(level, parentCode), ct))).WithTags("Reference");

        return endpoints;
    }
}
