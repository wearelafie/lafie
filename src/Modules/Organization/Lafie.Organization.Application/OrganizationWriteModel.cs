using Lafie.Organization.Domain;

namespace Lafie.Organization.Application;

/// <summary>Données d'écriture d'un établissement (création/màj), partagées commande ↔ API.</summary>
public sealed record OrganizationWriteModel(
    // Cœur (requis)
    string Code,
    string OfficialName,
    string FacilityTypeCode,
    string OwnershipTypeCode,
    string CountryCode,
    // Identification
    string? DisplayName = null,
    string? Abbreviation = null,
    string? Description = null,
    string? LogoUrl = null,
    string? WebsiteUrl = null,
    DateOnly? EstablishedOn = null,
    Guid? ParentOrganizationId = null,
    // Légal
    string? LegalName = null,
    string? LegalEntityTypeCode = null,
    string? RegistrationNumber = null,
    string? NationalFacilityCode = null,
    string? LicenseNumber = null,
    DateOnly? LicenseIssuedOn = null,
    DateOnly? LicenseExpiresOn = null,
    string? AccreditationStatusCode = null,
    string? TaxIdentificationNumber = null,
    // Adresse
    string? RegionCode = null,
    string? PrefectureCode = null,
    string? CommuneCode = null,
    string? Locality = null,
    string? DistrictOrNeighborhood = null,
    string? StreetAddress = null,
    string? PostalCode = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    decimal? AltitudeMeters = null,
    string? LocationAccuracy = null,
    string? TimeZoneId = null,
    // Exploitation
    DateOnly? OpeningDate = null,
    string? DefaultCurrencyCode = null,
    string? DefaultLanguageCode = null,
    bool EmergencyServicesAvailable = false,
    bool PublicFacing = false,
    // Contact primaire
    string? Phone = null,
    string? Email = null)
{
    public FacilityIdentity Identity() => new(DisplayName, Abbreviation, Description, LogoUrl, WebsiteUrl, EstablishedOn, ParentOrganizationId);

    public FacilityAddress Address() => new(RegionCode, PrefectureCode, CommuneCode, Locality, DistrictOrNeighborhood,
        StreetAddress, PostalCode, Latitude, Longitude, AltitudeMeters, LocationAccuracy, TimeZoneId ?? "Africa/Lome");

    public FacilityLegal Legal() => new(LegalName, LegalEntityTypeCode, RegistrationNumber, NationalFacilityCode,
        LicenseNumber, LicenseIssuedOn, LicenseExpiresOn, AccreditationStatusCode, TaxIdentificationNumber);

    public FacilityOperations Operations() => new(OpeningDate,
        DefaultCurrencyCode ?? "XOF", DefaultLanguageCode ?? "fr", EmergencyServicesAvailable, PublicFacing);

    public FacilityPrimaryContact Contact() => new(Phone, Email);
}
