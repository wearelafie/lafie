namespace Lafie.Organization.Domain;

/// <summary>Type d'unité de soins (extensible).</summary>
public enum CareUnitType
{
    Hospitalization = 1,
    IntensiveCare = 2,
    Resuscitation = 3,
    Emergency = 4,
    Maternity = 5,
    Ambulatory = 6,
    OperatingTheatre = 7,
    Consultation = 8,
    DayHospital = 9,
    Other = 99,
}
