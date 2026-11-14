using Lafie.Organization.Application.References;
using Lafie.Web.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lafie.Organization.Api;

public sealed record CreateReferenceValueRequest(string Category, string Code, string LabelFr, string LabelEn, int DisplayOrder, bool Active);
public sealed record UpdateReferenceValueRequest(string LabelFr, string LabelEn, int DisplayOrder, bool Active);
public sealed record SetActiveRequest(bool Active);
public sealed record CreateGeoDivisionRequest(string Level, string Code, string Name, string? ParentCode, int DisplayOrder);
public sealed record UpdateGeoDivisionRequest(string Name, string? ParentCode, int DisplayOrder);

/// <summary>Endpoints HTTP des données de référence (listes de codes + géographie) : lecture + administration.</summary>
public static class ReferenceEndpoints
{
    public static IEndpointRouteBuilder MapReferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // --- Lecture (formulaires) : codes actifs uniquement ---
        endpoints.MapGet("/reference-values/{category}", async (string category, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetReferenceValuesQuery(category), ct))).WithTags("Reference").RequirePermission("reference.read");

        endpoints.MapGet("/geo-divisions", async (string level, string? parentCode, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetGeoDivisionsQuery(level, parentCode), ct))).WithTags("Reference").RequirePermission("reference.read");

        // --- Administration : valeurs de référence ---
        var admin = endpoints.MapGroup("/reference-admin").WithTags("ReferenceAdmin");

        admin.MapGet("/categories", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetReferenceCategoriesQuery(), ct))).RequirePermission("reference.read");

        admin.MapGet("/values/{category}", async (string category, string? search, bool? includeInactive, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetReferenceValuesAdminQuery(category, search, includeInactive ?? false), ct))).RequirePermission("reference.read");

        admin.MapPost("/values", async (CreateReferenceValueRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateReferenceValueCommand(r.Category, r.Code, r.LabelFr, r.LabelEn, r.DisplayOrder, r.Active), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("reference.manage");

        admin.MapPut("/values/{category}/{code}", async (string category, string code, UpdateReferenceValueRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateReferenceValueCommand(category, code, r.LabelFr, r.LabelEn, r.DisplayOrder, r.Active), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("reference.manage");

        admin.MapPut("/values/{category}/{code}/active", async (string category, string code, SetActiveRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetReferenceValueActiveCommand(category, code, r.Active), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("reference.manage");

        admin.MapDelete("/values/{category}/{code}", async (string category, string code, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteReferenceValueCommand(category, code), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("reference.manage");

        // --- Administration : divisions géographiques ---
        admin.MapPost("/geo", async (CreateGeoDivisionRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateGeoDivisionCommand(r.Level, r.Code, r.Name, r.ParentCode, r.DisplayOrder), ct);
            return result.IsSuccess ? Results.Created($"/geo-divisions/{result.Value}", new { id = result.Value }) : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("reference.manage");

        admin.MapPut("/geo/{id:guid}", async (Guid id, UpdateGeoDivisionRequest r, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateGeoDivisionCommand(id, r.Name, r.ParentCode, r.DisplayOrder), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequirePermission("reference.manage");

        admin.MapDelete("/geo/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteGeoDivisionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.Conflict(new { error = result.Error });
        }).RequirePermission("reference.manage");

        return endpoints;
    }
}
