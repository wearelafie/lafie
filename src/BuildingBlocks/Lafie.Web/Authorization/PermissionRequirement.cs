using Microsoft.AspNetCore.Authorization;

namespace Lafie.Web.Authorization;

/// <summary>Exige une permission précise (claim "perm"), le joker "*" l'emportant.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var perms = context.User.FindAll("perm");
        foreach (var p in perms)
        {
            if (p.Value == "*" || p.Value == requirement.Permission)
            {
                context.Succeed(requirement);
                break;
            }
        }
        return Task.CompletedTask;
    }
}
