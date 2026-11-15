using Lafie.Core.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Web.Context;

/// <summary>Cloisonnement multi-tenant des endpoints adressant une ressource par identifiant.</summary>
public static class FacilityScopeExtensions
{
    /// <summary>
    /// Ajoute au groupe un filtre qui résout l'établissement de la ressource ciblée par le
    /// paramètre de route <paramref name="routeParam"/> (via <paramref name="resolveFacility"/>, côté
    /// serveur), puis vérifie qu'il correspond à l'établissement actif du caller
    /// (<see cref="IEstablishmentGuard.EnsureActiveFacility"/> — bypass super-admin perm "*").
    /// Si la ressource est introuvable (établissement null) on laisse passer : le handler renvoie 404.
    /// </summary>
    public static RouteGroupBuilder RequireFacilityScope(
        this RouteGroupBuilder group,
        string routeParam,
        Func<Guid, IServiceProvider, CancellationToken, ValueTask<Guid?>> resolveFacility)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            if (http.Request.RouteValues.TryGetValue(routeParam, out var raw)
                && Guid.TryParse(raw?.ToString(), out var resourceId))
            {
                var facilityId = await resolveFacility(resourceId, http.RequestServices, http.RequestAborted);
                if (facilityId is { } facility)
                    http.RequestServices.GetRequiredService<IEstablishmentGuard>().EnsureActiveFacility(facility);
            }

            return await next(context);
        });

        return group;
    }

    /// <summary>
    /// Variante résolvant l'établissement via un unique service <typeparamref name="TResolver"/>
    /// (typiquement le repository de la feature) — cas courant à un seul aller en base.
    /// </summary>
    public static RouteGroupBuilder RequireFacilityScope<TResolver>(
        this RouteGroupBuilder group,
        string routeParam,
        Func<TResolver, Guid, CancellationToken, Task<Guid?>> resolveFacility)
        where TResolver : notnull
        => group.RequireFacilityScope(routeParam,
            (resourceId, sp, ct) => new ValueTask<Guid?>(
                resolveFacility(sp.GetRequiredService<TResolver>(), resourceId, ct)));
}
