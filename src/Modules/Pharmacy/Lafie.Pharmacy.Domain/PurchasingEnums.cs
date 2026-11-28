namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Cycle de vie d'un bon de commande fournisseur.
/// Draft → Ordered → (PartiallyReceived) → Received ; annulable tant que non totalement reçu.
/// </summary>
public enum PurchaseOrderStatus
{
    Draft,
    Ordered,
    PartiallyReceived,
    Received,
    Cancelled,
}
