using Lafie.Identity.Application.Security;
using Lafie.Identity.Application.Users;
using Lafie.Identity.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Identity.Api;

/// <summary>Point d'entrée DI du module Identity.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        return services;
    }
}
