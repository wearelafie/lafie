using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>
/// Agrégat établissement de santé (HealthcareFacility). Identité propre ; les lieux physiques
/// (Building/Floor/Space) et les données complémentaires (contacts, identifiants…) vivent dans
/// leurs propres tables. Les `*Code` référencent des référentiels (tables lookup à venir).
/// </summary>
public sealed class Organization : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    // Identification
    public string Code { get; private set; }
    public string OfficialName { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Abbreviation { get; private set; }
    public string? Description { get; private set; }
    public string FacilityTypeCode { get; private set; }
    public string OwnershipTypeCode { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public DateOnly? EstablishedOn { get; private set; }
    public Guid? ParentOrganizationId { get; private set; }

    // Légal / administratif
    public string? LegalName { get; private set; }
    public string? LegalEntityTypeCode { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? NationalFacilityCode { get; private set; }
    public string? LicenseNumber { get; private set; }
    public DateOnly? LicenseIssuedOn { get; private set; }
    public DateOnly? LicenseExpiresOn { get; private set; }
    public string? AccreditationStatusCode { get; private set; }
    public string? TaxIdentificationNumber { get; private set; }

    // Adresse / géolocalisation
    public string CountryCode { get; private set; }
    public string? RegionCode { get; private set; }
    public string? PrefectureCode { get; private set; }
    public string? CommuneCode { get; private set; }
    public string? Locality { get; private set; }
    public string? DistrictOrNeighborhood { get; private set; }
    public string? StreetAddress { get; private set; }
    public string? PostalCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal? AltitudeMeters { get; private set; }
    public string? LocationAccuracy { get; private set; }
    public string TimeZoneId { get; private set; }

    // Exploitation
    public DateOnly? OpeningDate { get; private set; }
    public string DefaultCurrencyCode { get; private set; }
    public string DefaultLanguageCode { get; private set; }
    public bool EmergencyServicesAvailable { get; private set; }
    public bool PublicFacing { get; private set; }
    public OperationalStatus OperationalStatus { get; private set; }
    public ConfigurationStatus ConfigurationStatus { get; private set; }

    // Contact primaire (provisoire)
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public DateTimeOffset? ArchivedOnUtc { get; private set; }
    public bool IsArchived => ArchivedOnUtc is not null;

    private Organization(Guid id)
    {
        Id = id;
        Code = string.Empty;
        OfficialName = string.Empty;
        FacilityTypeCode = string.Empty;
        OwnershipTypeCode = string.Empty;
        CountryCode = "TG";
        TimeZoneId = "Africa/Lome";
        DefaultCurrencyCode = "XOF";
        DefaultLanguageCode = "fr";
    }

    public static Result<Organization> Create(
        string code, string officialName, string facilityTypeCode, string ownershipTypeCode, string countryCode,
        FacilityIdentity? identity = null, FacilityAddress? address = null, FacilityLegal? legal = null,
        FacilityOperations? operations = null, FacilityPrimaryContact? contact = null)
    {
        var o = new Organization(Guid.NewGuid());
        var apply = o.Apply(code, officialName, facilityTypeCode, ownershipTypeCode, countryCode,
            identity ?? new(), address ?? new(), legal ?? new(), operations ?? new(), contact ?? new());
        if (apply.IsFailure) return Result.Failure<Organization>(apply.Error!);

        o.OperationalStatus = OperationalStatus.Active;
        o.ConfigurationStatus = ConfigurationStatus.Draft;
        o.CreatedOnUtc = DateTimeOffset.UtcNow;
        o.AddDomainEvent(DomainEvent.Create((id, on) => new OrganizationCreated(id, on, o.Id, o.OfficialName, o.FacilityTypeCode)));
        return Result.Success(o);
    }

    public Result Update(
        string code, string officialName, string facilityTypeCode, string ownershipTypeCode, string countryCode,
        FacilityIdentity identity, FacilityAddress address, FacilityLegal legal, FacilityOperations operations, FacilityPrimaryContact contact)
    {
        var apply = Apply(code, officialName, facilityTypeCode, ownershipTypeCode, countryCode, identity, address, legal, operations, contact);
        if (apply.IsFailure) return apply;
        Touch();
        return Result.Success();
    }

    public void ChangeOperationalStatus(OperationalStatus status)
    {
        OperationalStatus = status;
        Touch();
    }

    public void SetConfigurationStatus(ConfigurationStatus status)
    {
        ConfigurationStatus = status;
        Touch();
    }

    public void Archive()
    {
        ArchivedOnUtc ??= DateTimeOffset.UtcNow;
        Touch();
    }

    public void Restore()
    {
        ArchivedOnUtc = null;
        Touch();
    }

    // --- application commune des champs + validation ---
    private Result Apply(
        string code, string officialName, string facilityTypeCode, string ownershipTypeCode, string countryCode,
        FacilityIdentity identity, FacilityAddress address, FacilityLegal legal, FacilityOperations operations, FacilityPrimaryContact contact)
    {
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure("Le code est obligatoire.");
        if (code.Trim().Length > CodeMaxLength) return Result.Failure($"Le code dépasse {CodeMaxLength} caractères.");
        if (string.IsNullOrWhiteSpace(officialName)) return Result.Failure("Le nom officiel est obligatoire.");
        if (officialName.Trim().Length > NameMaxLength) return Result.Failure($"Le nom dépasse {NameMaxLength} caractères.");
        if (string.IsNullOrWhiteSpace(facilityTypeCode)) return Result.Failure("Le type d'établissement est obligatoire.");
        if (string.IsNullOrWhiteSpace(ownershipTypeCode)) return Result.Failure("Le type de propriété est obligatoire.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            return Result.Failure("Le code pays (ISO alpha-2) est obligatoire.");

        var geo = ValidateGeo(address.Latitude, address.Longitude);
        if (geo.IsFailure) return geo;

        if (legal.LicenseIssuedOn is { } iss && legal.LicenseExpiresOn is { } exp && exp < iss)
            return Result.Failure("La date d'expiration de la licence précède sa délivrance.");

        Code = code.Trim();
        OfficialName = officialName.Trim();
        FacilityTypeCode = facilityTypeCode.Trim();
        OwnershipTypeCode = ownershipTypeCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();

        DisplayName = Blank(identity.DisplayName);
        Abbreviation = Blank(identity.Abbreviation);
        Description = Blank(identity.Description);
        LogoUrl = Blank(identity.LogoUrl);
        WebsiteUrl = Blank(identity.WebsiteUrl);
        EstablishedOn = identity.EstablishedOn;
        ParentOrganizationId = identity.ParentOrganizationId;

        LegalName = Blank(legal.LegalName);
        LegalEntityTypeCode = Blank(legal.LegalEntityTypeCode);
        RegistrationNumber = Blank(legal.RegistrationNumber);
        NationalFacilityCode = Blank(legal.NationalFacilityCode);
        LicenseNumber = Blank(legal.LicenseNumber);
        LicenseIssuedOn = legal.LicenseIssuedOn;
        LicenseExpiresOn = legal.LicenseExpiresOn;
        AccreditationStatusCode = Blank(legal.AccreditationStatusCode);
        TaxIdentificationNumber = Blank(legal.TaxIdentificationNumber);

        RegionCode = Blank(address.RegionCode);
        PrefectureCode = Blank(address.PrefectureCode);
        CommuneCode = Blank(address.CommuneCode);
        Locality = Blank(address.Locality);
        DistrictOrNeighborhood = Blank(address.DistrictOrNeighborhood);
        StreetAddress = Blank(address.StreetAddress);
        PostalCode = Blank(address.PostalCode);
        Latitude = address.Latitude;
        Longitude = address.Longitude;
        AltitudeMeters = address.AltitudeMeters;
        LocationAccuracy = Blank(address.LocationAccuracy);
        TimeZoneId = string.IsNullOrWhiteSpace(address.TimeZoneId) ? "Africa/Lome" : address.TimeZoneId.Trim();

        OpeningDate = operations.OpeningDate;
        DefaultCurrencyCode = string.IsNullOrWhiteSpace(operations.DefaultCurrencyCode) ? "XOF" : operations.DefaultCurrencyCode.Trim().ToUpperInvariant();
        DefaultLanguageCode = string.IsNullOrWhiteSpace(operations.DefaultLanguageCode) ? "fr" : operations.DefaultLanguageCode.Trim();
        EmergencyServicesAvailable = operations.EmergencyServicesAvailable;
        PublicFacing = operations.PublicFacing;

        Phone = Blank(contact.Phone);
        Email = Blank(contact.Email);

        return Result.Success();
    }

    private static Result ValidateGeo(decimal? lat, decimal? lng)
    {
        if (lat.HasValue != lng.HasValue)
            return Result.Failure("Latitude et longitude doivent être renseignées ensemble.");
        if (lat is < -90 or > 90) return Result.Failure("Latitude hors plage (-90..90).");
        if (lng is < -180 or > 180) return Result.Failure("Longitude hors plage (-180..180).");
        return Result.Success();
    }

    public static Organization Rehydrate(Action<Organization> hydrate)
    {
        var o = new Organization(Guid.Empty);
        hydrate(o);
        return o;
    }

    // Setters internes pour la réhydratation (repository).
    internal void SetId(Guid id) => Id = id;
    internal void HydrateIdentity(string code, string officialName, string? displayName, string? abbreviation, string? description,
        string facilityTypeCode, string ownershipTypeCode, string? logoUrl, string? websiteUrl, DateOnly? establishedOn, Guid? parentOrganizationId)
    { Code = code; OfficialName = officialName; DisplayName = displayName; Abbreviation = abbreviation; Description = description;
      FacilityTypeCode = facilityTypeCode; OwnershipTypeCode = ownershipTypeCode; LogoUrl = logoUrl; WebsiteUrl = websiteUrl;
      EstablishedOn = establishedOn; ParentOrganizationId = parentOrganizationId; }
    internal void HydrateLegal(string? legalName, string? legalEntityTypeCode, string? registrationNumber, string? nationalFacilityCode,
        string? licenseNumber, DateOnly? licenseIssuedOn, DateOnly? licenseExpiresOn, string? accreditationStatusCode, string? taxId)
    { LegalName = legalName; LegalEntityTypeCode = legalEntityTypeCode; RegistrationNumber = registrationNumber; NationalFacilityCode = nationalFacilityCode;
      LicenseNumber = licenseNumber; LicenseIssuedOn = licenseIssuedOn; LicenseExpiresOn = licenseExpiresOn; AccreditationStatusCode = accreditationStatusCode; TaxIdentificationNumber = taxId; }
    internal void HydrateAddress(string countryCode, string? regionCode, string? prefectureCode, string? communeCode, string? locality,
        string? district, string? streetAddress, string? postalCode, decimal? latitude, decimal? longitude, decimal? altitude, string? locationAccuracy, string timeZoneId)
    { CountryCode = countryCode; RegionCode = regionCode; PrefectureCode = prefectureCode; CommuneCode = communeCode; Locality = locality;
      DistrictOrNeighborhood = district; StreetAddress = streetAddress; PostalCode = postalCode; Latitude = latitude; Longitude = longitude;
      AltitudeMeters = altitude; LocationAccuracy = locationAccuracy; TimeZoneId = timeZoneId; }
    internal void HydrateOperations(DateOnly? openingDate, string currency, string language, bool emergency, bool publicFacing,
        OperationalStatus operational, ConfigurationStatus configuration, string? phone, string? email,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc, DateTimeOffset? archivedOnUtc)
    { OpeningDate = openingDate; DefaultCurrencyCode = currency; DefaultLanguageCode = language; EmergencyServicesAvailable = emergency; PublicFacing = publicFacing;
      OperationalStatus = operational; ConfigurationStatus = configuration; Phone = phone; Email = email;
      CreatedOnUtc = createdOnUtc; UpdatedOnUtc = updatedOnUtc; ArchivedOnUtc = archivedOnUtc; }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
