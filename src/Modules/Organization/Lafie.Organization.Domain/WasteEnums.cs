namespace Lafie.Organization.Domain;

/// <summary>Filière de déchet (flux), déterminant le conditionnement et le traitement réglementaire.</summary>
public enum WasteStream
{
    /// <summary>Déchets d'activités de soins à risques infectieux.</summary>
    Dasri = 1,
    /// <summary>Objets perforants, coupants, tranchants (OPCT).</summary>
    Sharps = 2,
    /// <summary>Déchets cytotoxiques / cytostatiques.</summary>
    Cytotoxic = 3,
    /// <summary>Pièces anatomiques.</summary>
    Anatomical = 4,
    /// <summary>Déchets assimilés aux ordures ménagères (DAOM).</summary>
    Daom = 5,
    /// <summary>Déchets chimiques.</summary>
    Chemical = 6,
    /// <summary>Déchets pharmaceutiques.</summary>
    Pharmaceutical = 7,
}

/// <summary>Cycle de vie d'une collecte pesée.</summary>
public enum WasteCollectionStatus
{
    Collected = 1,
    /// <summary>Rattachée à un bordereau d'élimination.</summary>
    Assigned = 2,
    /// <summary>Éliminée (bordereau traité).</summary>
    Eliminated = 3,
    Cancelled = 4,
}

/// <summary>Cycle de vie d'un bordereau d'élimination.</summary>
public enum WasteSlipStatus
{
    Draft = 1,
    Shipped = 2,
    Treated = 3,
    Cancelled = 4,
}

/// <summary>Mode de traitement de la filière.</summary>
public enum WasteTreatment
{
    Incineration = 1,
    Autoclaving = 2,
    Landfill = 3,
    Other = 4,
}
