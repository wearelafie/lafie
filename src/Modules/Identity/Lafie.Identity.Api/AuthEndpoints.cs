using Lafie.Identity.Application.Auth;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Identity.Api;

public sealed record LoginRequest(string Login, string Password);
public sealed record SelectEstablishmentRequest(Guid FacilityId);

/// <summary>Endpoints HTTP d'authentification.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", async (LoginRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LoginCommand(request.Login, request.Password), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
        }).AllowAnonymous().WithTags("Auth");

        endpoints.MapPost("/auth/select-establishment", async (SelectEstablishmentRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SelectEstablishmentCommand(request.FacilityId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status403Forbidden);
        }).RequireAuthorization().WithTags("Auth");

        endpoints.MapGet("/me", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMeQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).RequireAuthorization().WithTags("Auth");

        return endpoints;
    }
}
