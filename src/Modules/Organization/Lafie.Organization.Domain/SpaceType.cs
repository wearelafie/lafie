namespace Lafie.Organization.Domain;

/// <summary>Type d'espace physique (extensible).</summary>
public enum SpaceType
{
    PatientRoom = 1,
    ConsultationRoom = 2,
    TreatmentRoom = 3,
    EmergencyRoom = 4,
    OperatingRoom = 5,
    RecoveryRoom = 6,
    IntensiveCareRoom = 7,
    Laboratory = 8,
    ImagingRoom = 9,
    Pharmacy = 10,
    Office = 11,
    WaitingRoom = 12,
    TechnicalRoom = 13,
    Storage = 14,
    Other = 99,
}
