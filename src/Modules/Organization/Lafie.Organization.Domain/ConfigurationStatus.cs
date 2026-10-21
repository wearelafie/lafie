namespace Lafie.Organization.Domain;

/// <summary>État de configuration de l'établissement dans Lafie (indépendant de son état opérationnel/administratif).</summary>
public enum ConfigurationStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
}
