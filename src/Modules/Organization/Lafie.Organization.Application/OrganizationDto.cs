namespace Lafie.Organization.Application;

/// <summary>Projection lecture d'un établissement (mappée par Dapper, par nom de colonne).</summary>
public sealed class OrganizationDto
{
    public Guid Id { get; init; }
    // Identification
    public string Code { get; init; } = string.Empty;
    public string OfficialName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Abbreviation { get; init; }
    public string? Description { get; init; }
    public string FacilityTypeCode { get; init; } = string.Empty;
    public string OwnershipTypeCode { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public DateOnly? EstablishedOn { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    // Légal
    public string? LegalName { get; init; }
    public string? LegalEntityTypeCode { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? NationalFacilityCode { get; init; }
    public string? LicenseNumber { get; init; }
    public DateOnly? LicenseIssuedOn { get; init; }
    public DateOnly? LicenseExpiresOn { get; init; }
    public string? AccreditationStatusCode { get; init; }
    public string? TaxIdentificationNumber { get; init; }
    // Adresse
    public string CountryCode { get; init; } = string.Empty;
    public string? RegionCode { get; init; }
    public string? PrefectureCode { get; init; }
    public string? CommuneCode { get; init; }
    public string? Locality { get; init; }
    public string? DistrictOrNeighborhood { get; init; }
    public string? StreetAddress { get; init; }
    public string? PostalCode { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public decimal? AltitudeMeters { get; init; }
    public string? LocationAccuracy { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    // Exploitation
    public DateOnly? OpeningDate { get; init; }
    public string DefaultCurrencyCode { get; init; } = string.Empty;
    public string DefaultLanguageCode { get; init; } = string.Empty;
    public bool EmergencyServicesAvailable { get; init; }
    public bool PublicFacing { get; init; }
    public string OperationalStatus { get; init; } = string.Empty;
    public string ConfigurationStatus { get; init; } = string.Empty;
    // Contact
    public string? Phone { get; init; }
    public string? Email { get; init; }

    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? ArchivedOnUtc { get; init; }
}
