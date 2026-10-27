namespace Lafie.Identity.Application.Security;

/// <summary>Hachage et vérification des mots de passe.</summary>
public interface IPasswordHasher
{
    string Algorithm { get; }
    string Hash(string password);
    bool Verify(string hash, string password);
}
