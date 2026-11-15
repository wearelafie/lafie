namespace Lafie.Organization.Domain;

/// <summary>Nature de la cible d'un ticket de maintenance (équipement ou espace physique).</summary>
public enum TicketTargetType
{
    Equipment = 1,
    Space = 2,
}

/// <summary>Niveau de criticité d'un incident technique.</summary>
public enum TicketCriticality
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>Cycle de vie d'un ticket de maintenance (curatif).</summary>
public enum TicketStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
}

/// <summary>Type d'intervenant affecté à un ticket.</summary>
public enum TicketAssigneeType
{
    Internal = 1,
    External = 2,
}
