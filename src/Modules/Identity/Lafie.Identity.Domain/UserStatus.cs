namespace Lafie.Identity.Domain;

/// <summary>État d'un compte utilisateur.</summary>
public enum UserStatus
{
    /// <summary>Invité, pas encore activé (premier mot de passe à définir).</summary>
    Invited = 1,
    Active = 2,
    Suspended = 3,
    /// <summary>Verrouillé (trop d'échecs de connexion).</summary>
    Locked = 4,
    Deactivated = 5,
}
