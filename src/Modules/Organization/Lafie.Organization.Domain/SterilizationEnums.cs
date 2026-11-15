namespace Lafie.Organization.Domain;

/// <summary>État de cycle de vie d'une boîte d'instruments (CSSD).</summary>
public enum InstrumentSetStatus
{
    /// <summary>Stérile, prête à l'emploi.</summary>
    Sterile = 1,
    /// <summary>Affectée / utilisée sur une intervention.</summary>
    InUse = 2,
    /// <summary>Souillée, à retraiter.</summary>
    Soiled = 3,
    /// <summary>Dans un cycle d'autoclave en cours.</summary>
    InSterilization = 4,
    /// <summary>En quarantaine (cycle échoué / contrôle non conforme).</summary>
    Quarantine = 5,
    /// <summary>Réformée.</summary>
    Retired = 6,
}

/// <summary>Résultat / avancement d'un cycle d'autoclave.</summary>
public enum SterilizationCycleStatus
{
    Planned = 1,
    Running = 2,
    Passed = 3,
    Failed = 4,
}

/// <summary>Cycle de vie d'une affectation d'une boîte à une intervention.</summary>
public enum SetUsageStatus
{
    Allocated = 1,
    Used = 2,
    Returned = 3,
    Cancelled = 4,
}
