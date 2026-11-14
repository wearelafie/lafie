namespace Lafie.Organization.Domain;

/// <summary>Nature d'une intervention de maintenance sur un équipement.</summary>
public enum MaintenanceType
{
    /// <summary>Maintenance préventive (planifiée, périodique).</summary>
    Preventive = 1,
    /// <summary>Maintenance corrective / réparation (suite à panne).</summary>
    Corrective = 2,
}
