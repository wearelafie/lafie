namespace Lafie.Identity.Domain;

/// <summary>Nature d'un compte utilisateur.</summary>
public enum UserType
{
    /// <summary>Personnel administratif/support.</summary>
    Staff = 1,
    /// <summary>Professionnel de santé (lié à un Practitioner).</summary>
    Practitioner = 2,
    /// <summary>Compte de service (intégrations, machines).</summary>
    ServiceAccount = 3,
    /// <summary>Patient (portail — futur).</summary>
    Patient = 4,
}
