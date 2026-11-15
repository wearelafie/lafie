namespace Lafie.Organization.Domain;

/// <summary>
/// État opérationnel runtime d'un espace (temps réel), distinct du statut de cycle de vie <see cref="SpaceStatus"/>.
/// Piloté par les flux exploitation (GMAO, hygiène) : un ticket bloquant → Blocked, une sortie → Cleaning, etc.
/// </summary>
public enum SpaceOperationalState
{
    Available = 1,
    Occupied = 2,
    Cleaning = 3,
    Blocked = 4,
}
