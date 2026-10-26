namespace Lafie.Organization.Domain;

/// <summary>Catégorie d'un service / département d'un établissement.</summary>
public enum ServiceCategory
{
    /// <summary>Service clinique (soins aux patients) : cardiologie, pédiatrie…</summary>
    Clinical = 1,
    /// <summary>Service médico-technique : laboratoire, imagerie, pharmacie…</summary>
    MedicoTechnical = 2,
    /// <summary>Service administratif : direction, finances, RH…</summary>
    Administrative = 3,
    /// <summary>Service hôtelier/logistique : restauration, lingerie, maintenance…</summary>
    Hosting = 4,
}
