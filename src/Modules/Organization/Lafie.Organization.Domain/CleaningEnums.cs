namespace Lafie.Organization.Domain;

/// <summary>Niveau de protocole de bio-nettoyage, adapté au risque infectieux.</summary>
public enum CleaningProtocol
{
    /// <summary>Entretien courant entre deux occupations.</summary>
    Standard = 1,
    /// <summary>Nettoyage approfondi (précautions complémentaires contact).</summary>
    Deep = 2,
    /// <summary>Bio-nettoyage terminal (après patient infectieux / sortie, désinfection complète).</summary>
    Terminal = 3,
}

/// <summary>Événement déclencheur d'une tâche de bio-nettoyage.</summary>
public enum CleaningTrigger
{
    PostDischarge = 1,
    Scheduled = 2,
    OnDemand = 3,
}

/// <summary>Cycle de vie d'une tâche de bio-nettoyage.</summary>
public enum CleaningStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}
