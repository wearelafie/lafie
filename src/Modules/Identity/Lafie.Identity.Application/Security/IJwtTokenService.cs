using Lafie.Identity.Domain;

namespace Lafie.Identity.Application.Security;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>Émission des jetons d'accès (JWT).</summary>
public interface IJwtTokenService
{
    AccessToken Create(User user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions, Guid? activeFacilityId);
}
