using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Web.Modules;

/// <summary>
/// Contrat d'un module applicatif : enregistrement des services (DI) et
/// mapping de ses endpoints. Le host compose les modules via ce contrat.
/// </summary>
public interface IModule
{
    IServiceCollection RegisterServices(IServiceCollection services);

    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
