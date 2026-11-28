namespace Lafie.Pharmacy.Domain;

/// <summary>Forme galénique d'un médicament (présentation pharmaceutique).</summary>
public enum MedicationForm
{
    Tablet,       // Comprimé
    Capsule,      // Gélule / capsule
    Syrup,        // Sirop
    Solution,     // Solution buvable
    Injectable,   // Injectable (ampoule, flacon)
    Infusion,     // Perfusion
    Cream,        // Crème
    Ointment,     // Pommade
    Drops,        // Gouttes
    Suppository,  // Suppositoire
    Inhaler,      // Inhalateur / aérosol
    Patch,        // Patch transdermique
    Powder,       // Poudre
    Other,        // Autre
}

/// <summary>Cycle de vie d'une fiche médicament au catalogue d'un établissement.</summary>
public enum MedicationStatus
{
    Active,     // Référencé, utilisable
    Inactive,   // Déréférencé (conservé pour l'historique)
}
