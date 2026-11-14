namespace Lafie.Organization.Domain;

/// <summary>État opérationnel d'un équipement (actif biomédical / technique).</summary>
public enum EquipmentStatus
{
    /// <summary>En service, opérationnel.</summary>
    Operational = 1,
    /// <summary>En maintenance (préventive ou corrective) ou en réparation.</summary>
    UnderMaintenance = 2,
    /// <summary>Hors service (en panne, indisponible).</summary>
    OutOfService = 3,
    /// <summary>Réformé / mis au rebut (fin de vie).</summary>
    Decommissioned = 4,
}
