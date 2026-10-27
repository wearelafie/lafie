using Lafie.Core.Common;
using Lafie.Core.Context;
using Lafie.Identity.Application.Users;
using Mediator;

namespace Lafie.Identity.Application.Auth;

/// <summary>Profil de l'utilisateur actuellement authentifié (scopé à l'établissement actif).</summary>
public sealed record GetMeQuery : IRequest<Result<MeDto>>;

public sealed class GetMeHandler(IUserRepository users, ICurrentUser currentUser)
    : IRequestHandler<GetMeQuery, Result<MeDto>>
{
    public async ValueTask<Result<MeDto>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated())
            return Result.Failure<MeDto>("Non authentifié.");

        var user = await users.FindByIdAsync(currentUser.GetUserId(), cancellationToken);
        if (user is null) return Result.Failure<MeDto>("Compte introuvable.");

        Guid? active = Guid.TryParse(currentUser.GetTenant(), out var t) ? t : null;
        var establishments = await users.GetEstablishmentsAsync(user.Id, cancellationToken);
        var roles = await users.GetRoleCodesAsync(user.Id, active, cancellationToken);
        var permissions = await users.GetPermissionCodesAsync(user.Id, active, cancellationToken);
        var activeRef = establishments.FirstOrDefault(e => e.FacilityId == active);
        return Result.Success(LoginHandler.ToMe(user, activeRef, establishments, roles, permissions));
    }
}
