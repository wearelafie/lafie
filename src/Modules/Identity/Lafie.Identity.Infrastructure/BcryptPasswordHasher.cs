using Lafie.Identity.Application.Security;

namespace Lafie.Identity.Infrastructure;

/// <summary>Hachage des mots de passe via BCrypt.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Algorithm => "bcrypt";

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string hash, string password)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}
