namespace Lafie.Identity.Domain;

/// <summary>Fournisseur d'authentification d'un compte.</summary>
public enum AuthProvider
{
    /// <summary>Identifiants locaux (mot de passe haché).</summary>
    Local = 1,
    /// <summary>Fournisseur externe OIDC (ex. Keycloak).</summary>
    External = 2,
}
