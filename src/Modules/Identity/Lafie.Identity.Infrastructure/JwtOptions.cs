namespace Lafie.Identity.Infrastructure;

/// <summary>Configuration d'émission/validation des jetons JWT (section "Jwt").</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "lafie";
    public string Audience { get; set; } = "lafie";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 480;
}
