namespace Lafie.Organization.Domain;

/// <summary>Attributs d'identification complémentaires (optionnels).</summary>
public sealed record FacilityIdentity(
    string? DisplayName = null,
    string? Abbreviation = null,
    string? Description = null,
    string? LogoUrl = null,
    string? WebsiteUrl = null,
    DateOnly? EstablishedOn = null,
    Guid? ParentOrganizationId = null);

/// <summary>Adresse structurée + géolocalisation (CountryCode est dans le cœur de l'agrégat).</summary>
public sealed record FacilityAddress(
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
    string TimeZoneId = "Africa/Lome");

/// <summary>Informations juridiques/administratives (optionnelles ; jamais générées par Lafie).</summary>
public sealed record FacilityLegal(
    string? LegalName = null,
    string? LegalEntityTypeCode = null,
    string? RegistrationNumber = null,
    string? NationalFacilityCode = null,
    string? LicenseNumber = null,
    DateOnly? LicenseIssuedOn = null,
    DateOnly? LicenseExpiresOn = null,
    string? AccreditationStatusCode = null,
    string? TaxIdentificationNumber = null);

/// <summary>Informations d'exploitation (avec valeurs par défaut Togo).</summary>
public sealed record FacilityOperations(
    DateOnly? OpeningDate = null,
    string DefaultCurrencyCode = "XOF",
    string DefaultLanguageCode = "fr",
    bool EmergencyServicesAvailable = false,
    bool PublicFacing = false);

/// <summary>Contact primaire provisoire (la table FacilityContacts dédiée viendra plus tard).</summary>
public sealed record FacilityPrimaryContact(string? Phone = null, string? Email = null);
