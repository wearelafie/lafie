using Lafie.Core.Common;
using Lafie.Identity.Application.Security;
using Lafie.Identity.Application.Users;
using Lafie.Identity.Domain;
using Mediator;

namespace Lafie.Identity.Application.Auth;

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc, bool NeedsSelection, MeDto User);

/// <summary>Authentifie par e-mail ou nom d'utilisateur + mot de passe.</summary>
public sealed record LoginCommand(string Login, string Password) : IRequest<Result<LoginResponse>>;

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokens) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private const string InvalidError = "E-mail/nom d'utilisateur ou mot de passe incorrect.";

    public async ValueTask<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Login) || string.IsNullOrWhiteSpace(command.Password))
            return Result.Failure<LoginResponse>(InvalidError);

        var user = await users.FindByLoginAsync(command.Login.Trim(), cancellationToken);
        if (user is null || user.AuthProvider != AuthProvider.Local || user.PasswordHash is null || !user.CanLogin)
            return Result.Failure<LoginResponse>(InvalidError);

        if (!passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            user.RecordFailedLogin(MaxAttempts, LockDuration);
            await users.UpdateAsync(user, cancellationToken);
            return Result.Failure<LoginResponse>(InvalidError);
        }

        user.RecordSuccessfulLogin(null);
        await users.UpdateAsync(user, cancellationToken);

        var establishments = await users.GetEstablishmentsAsync(user.Id, cancellationToken);

        // Choix de l'établissement actif : 1 seul → auto ; défaut → auto ; 0 → contexte plateforme ; plusieurs → sélection requise.
        var active = establishments.Count == 1
            ? establishments[0]
            : establishments.FirstOrDefault(e => e.IsDefault);
        var needsSelection = active is null && establishments.Count > 1;

        var response = await BuildAsync(users, tokens, user, active?.FacilityId, establishments, needsSelection, cancellationToken);
        return Result.Success(response);
    }

    internal static async Task<LoginResponse> BuildAsync(
        IUserRepository users, IJwtTokenService tokens, User user, Guid? activeFacilityId,
        IReadOnlyList<EstablishmentRef> establishments, bool needsSelection, CancellationToken ct)
    {
        var roles = await users.GetRoleCodesAsync(user.Id, activeFacilityId, ct);
        var permissions = await users.GetPermissionCodesAsync(user.Id, activeFacilityId, ct);
        var token = tokens.Create(user, roles, permissions, activeFacilityId);
        var activeRef = establishments.FirstOrDefault(e => e.FacilityId == activeFacilityId);
        var me = ToMe(user, activeRef, establishments, roles, permissions);
        return new LoginResponse(token.Token, token.ExpiresAtUtc, needsSelection, me);
    }

    internal static MeDto ToMe(
        User u, EstablishmentRef? active, IReadOnlyList<EstablishmentRef> establishments,
        IReadOnlyList<string> roles, IReadOnlyList<string> permissions) => new(
        u.Id, u.Username, u.Email, u.DisplayName, u.FirstName, u.LastName, u.Title,
        u.UserType.ToString(), u.PreferredLanguage, u.AvatarUrl, u.PractitionerId, active, establishments, roles, permissions);
}
