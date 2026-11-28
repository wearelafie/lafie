namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Cycle de vie d'une prescription. Active dès la création ; Completed quand toutes les lignes
/// sont intégralement dispensées ; annulable tant qu'elle n'est pas totalement dispensée.
/// </summary>
public enum PrescriptionStatus
{
    Active,
    Completed,
    Cancelled,
}
