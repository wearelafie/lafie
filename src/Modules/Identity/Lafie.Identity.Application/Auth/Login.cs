using Lafie.Core.Common;
using Lafie.Identity.Application.Security;
using Lafie.Identity.Application.Users;
using Lafie.Identity.Domain;
using Mediator;

namespace Lafie.Identity.Application.Auth;

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc, MeDto User);

/// <summary>Authentifie par e-mail ou nom d'utilisateur + mot de passe.</summary>
public sealed record LoginCommand(string Login, string Password) : IRequest<Result<LoginResponse>>;

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokens) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    // Message volontairement générique (anti-énumération de comptes).
    private const string InvalidError = "E-mail/nom d'utilisateur ou mot de passe incorrect.";

    public async ValueTask<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Login) || string.IsNullOrWhiteSpace(command.Password))
            return Result.Failure<LoginResponse>(InvalidError);

        var user = await users.FindByLoginAsync(command.Login.Trim(), cancellationToken);
        if (user is null || user.AuthProvider != AuthProvider.Local || user.PasswordHash is null)
            return Result.Failure<LoginResponse>(InvalidError);

        if (!user.CanLogin)
            return Result.Failure<LoginResponse>(InvalidError);

        if (!passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            user.RecordFailedLogin(MaxAttempts, LockDuration);
            await users.UpdateAsync(user, cancellationToken);
            return Result.Failure<LoginResponse>(InvalidError);
        }

        user.RecordSuccessfulLogin(null);
        await users.UpdateAsync(user, cancellationToken);

        var roles = await users.GetRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await users.GetPermissionCodesAsync(user.Id, cancellationToken);
        var token = tokens.Create(user, roles, permissions);

        var me = ToMe(user, roles, permissions);
        return Result.Success(new LoginResponse(token.Token, token.ExpiresAtUtc, me));
    }

    internal static MeDto ToMe(User u, IReadOnlyList<string> roles, IReadOnlyList<string> permissions) => new(
        u.Id, u.Username, u.Email, u.DisplayName, u.FirstName, u.LastName, u.Title,
        u.UserType.ToString(), u.PreferredLanguage, u.AvatarUrl, u.PractitionerId, roles, permissions);
}
