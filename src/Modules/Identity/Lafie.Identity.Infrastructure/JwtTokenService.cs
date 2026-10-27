using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lafie.Identity.Application.Security;
using Lafie.Identity.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Lafie.Identity.Infrastructure;

/// <summary>Émission de JWT signés (HMAC-SHA256).</summary>
public sealed class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    public AccessToken Create(User user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions, Guid? activeFacilityId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("preferred_username", user.Username),
            new("name", user.DisplayName ?? user.Username),
        };
        if (activeFacilityId is { } facility)
        {
            // Tenant = établissement actif choisi à la connexion.
            claims.Add(new Claim("tenant", facility.ToString()));
            claims.Add(new Claim("facility", facility.ToString()));
        }
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var perm in permissions) claims.Add(new Claim("perm", perm));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
