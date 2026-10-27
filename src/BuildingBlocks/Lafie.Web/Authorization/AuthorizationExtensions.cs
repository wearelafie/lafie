using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Web.Authorization;

public static class AuthorizationExtensions
{
    /// <summary>Enregistre l'autorisation par permission (policies "perm:&lt;code&gt;").</summary>
    public static IServiceCollection AddLafiePermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }

    /// <summary>Exige la permission <paramref name="code"/> sur l'endpoint.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string code)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization($"perm:{code}");
}
