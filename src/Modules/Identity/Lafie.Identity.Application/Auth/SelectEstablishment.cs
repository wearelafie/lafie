using Lafie.Core.Common;
using Lafie.Core.Context;
using Lafie.Identity.Application.Security;
using Lafie.Identity.Application.Users;
using Mediator;

namespace Lafie.Identity.Application.Auth;

/// <summary>Choisit (ou change) l'établissement actif et émet un jeton scopé.</summary>
public sealed record SelectEstablishmentCommand(Guid FacilityId) : IRequest<Result<LoginResponse>>;

public sealed class SelectEstablishmentHandler(
    IUserRepository users,
    IJwtTokenService tokens,
    ICurrentUser currentUser) : IRequestHandler<SelectEstablishmentCommand, Result<LoginResponse>>
{
    public async ValueTask<Result<LoginResponse>> Handle(SelectEstablishmentCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated())
            return Result.Failure<LoginResponse>("Non authentifié.");

        var user = await users.FindByIdAsync(currentUser.GetUserId(), cancellationToken);
        if (user is null) return Result.Failure<LoginResponse>("Compte introuvable.");

        // On ne fait jamais confiance au facilityId client au-delà de « l'utilisateur en est-il membre ».
        if (!await users.IsMemberAsync(user.Id, command.FacilityId, cancellationToken))
            return Result.Failure<LoginResponse>("Établissement non autorisé.");

        var establishments = await users.GetEstablishmentsAsync(user.Id, cancellationToken);
        var response = await LoginHandler.BuildAsync(users, tokens, user, command.FacilityId, establishments, false, cancellationToken);
        return Result.Success(response);
    }
}
