using Lafie.Organization.Application;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Organization.Api;

/// <summary>Point d'entrée DI du module Organization.</summary>
public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        return services;
    }
}
