namespace Lafie.Pharmacy.Domain;

/// <summary>Nature d'un mouvement de stock (grand livre immuable).</summary>
public enum StockMovementType
{
    Receipt,     // Entrée (réception d'un lot)
    Issue,       // Sortie (consommation, FEFO)
    Adjustment,  // Ajustement d'inventaire (±)
    Disposal,    // Destruction / péremption (sortie)
}
