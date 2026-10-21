using System.Text;
using Dapper;
using Lafie.Core.Common;
using Lafie.Organization.Application;
using Lafie.Organization.Domain;
using Lafie.Persistence;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Infrastructure;

/// <summary>Persistance Dapper des établissements (schéma `organization`).</summary>
public sealed class OrganizationRepository(IDbConnectionFactory connectionFactory) : IOrganizationRepository
{
    private const string Columns = """
        id as "Id", code as "Code", official_name as "OfficialName", display_name as "DisplayName",
        abbreviation as "Abbreviation", description as "Description", facility_type_code as "FacilityTypeCode",
        ownership_type_code as "OwnershipTypeCode", logo_url as "LogoUrl", website_url as "WebsiteUrl",
        established_on as "EstablishedOn", parent_organization_id as "ParentOrganizationId",
        legal_name as "LegalName", legal_entity_type_code as "LegalEntityTypeCode", registration_number as "RegistrationNumber",
        national_facility_code as "NationalFacilityCode", license_number as "LicenseNumber", license_issued_on as "LicenseIssuedOn",
        license_expires_on as "LicenseExpiresOn", accreditation_status_code as "AccreditationStatusCode", tax_identification_number as "TaxIdentificationNumber",
        country_code as "CountryCode", region_code as "RegionCode", prefecture_code as "PrefectureCode", commune_code as "CommuneCode",
        locality as "Locality", district_or_neighborhood as "DistrictOrNeighborhood", street_address as "StreetAddress", postal_code as "PostalCode",
        latitude as "Latitude", longitude as "Longitude", altitude_meters as "AltitudeMeters", location_accuracy as "LocationAccuracy", time_zone_id as "TimeZoneId",
        opening_date as "OpeningDate", default_currency_code as "DefaultCurrencyCode", default_language_code as "DefaultLanguageCode",
        emergency_services_available as "EmergencyServicesAvailable", public_facing as "PublicFacing",
        operational_status as "OperationalStatus", configuration_status as "ConfigurationStatus",
        phone as "Phone", email as "Email", created_on_utc as "CreatedOnUtc", updated_on_utc as "UpdatedOnUtc"
        """;

    private const string InsertSql = """
        insert into organization.organizations (
            id, code, official_name, display_name, abbreviation, description, facility_type_code, ownership_type_code,
            logo_url, website_url, established_on, parent_organization_id,
            legal_name, legal_entity_type_code, registration_number, national_facility_code, license_number,
            license_issued_on, license_expires_on, accreditation_status_code, tax_identification_number,
            country_code, region_code, prefecture_code, commune_code, locality, district_or_neighborhood, street_address, postal_code,
            latitude, longitude, altitude_meters, location_accuracy, time_zone_id,
            opening_date, default_currency_code, default_language_code, emergency_services_available, public_facing,
            operational_status, configuration_status, phone, email, created_on_utc, updated_on_utc)
        values (
            @Id, @Code, @OfficialName, @DisplayName, @Abbreviation, @Description, @FacilityTypeCode, @OwnershipTypeCode,
            @LogoUrl, @WebsiteUrl, @EstablishedOn, @ParentOrganizationId,
            @LegalName, @LegalEntityTypeCode, @RegistrationNumber, @NationalFacilityCode, @LicenseNumber,
            @LicenseIssuedOn, @LicenseExpiresOn, @AccreditationStatusCode, @TaxIdentificationNumber,
            @CountryCode, @RegionCode, @PrefectureCode, @CommuneCode, @Locality, @DistrictOrNeighborhood, @StreetAddress, @PostalCode,
            @Latitude, @Longitude, @AltitudeMeters, @LocationAccuracy, @TimeZoneId,
            @OpeningDate, @DefaultCurrencyCode, @DefaultLanguageCode, @EmergencyServicesAvailable, @PublicFacing,
            @OperationalStatus, @ConfigurationStatus, @Phone, @Email, @CreatedOnUtc, @UpdatedOnUtc)
        """;

    private const string UpdateSql = """
        update organization.organizations set
            code = @Code, official_name = @OfficialName, display_name = @DisplayName, abbreviation = @Abbreviation,
            description = @Description, facility_type_code = @FacilityTypeCode, ownership_type_code = @OwnershipTypeCode,
            logo_url = @LogoUrl, website_url = @WebsiteUrl, established_on = @EstablishedOn, parent_organization_id = @ParentOrganizationId,
            legal_name = @LegalName, legal_entity_type_code = @LegalEntityTypeCode, registration_number = @RegistrationNumber,
            national_facility_code = @NationalFacilityCode, license_number = @LicenseNumber, license_issued_on = @LicenseIssuedOn,
            license_expires_on = @LicenseExpiresOn, accreditation_status_code = @AccreditationStatusCode, tax_identification_number = @TaxIdentificationNumber,
            country_code = @CountryCode, region_code = @RegionCode, prefecture_code = @PrefectureCode, commune_code = @CommuneCode,
            locality = @Locality, district_or_neighborhood = @DistrictOrNeighborhood, street_address = @StreetAddress, postal_code = @PostalCode,
            latitude = @Latitude, longitude = @Longitude, altitude_meters = @AltitudeMeters, location_accuracy = @LocationAccuracy, time_zone_id = @TimeZoneId,
            opening_date = @OpeningDate, default_currency_code = @DefaultCurrencyCode, default_language_code = @DefaultLanguageCode,
            emergency_services_available = @EmergencyServicesAvailable, public_facing = @PublicFacing,
            operational_status = @OperationalStatus, configuration_status = @ConfigurationStatus, phone = @Phone, email = @Email,
            updated_on_utc = @UpdatedOnUtc
        where id = @Id
        """;

    public async Task AddAsync(DomainOrganization o, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(InsertSql, ToParams(o), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(DomainOrganization o, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(UpdateSql, ToParams(o), cancellationToken: cancellationToken));
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"select {Columns} from organization.organizations where id = @id";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<OrganizationDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<DomainOrganization?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null) return null;
        return DomainOrganization.Rehydrate(o =>
        {
            o.SetId(dto.Id);
            o.HydrateIdentity(dto.Code, dto.OfficialName, dto.DisplayName, dto.Abbreviation, dto.Description,
                dto.FacilityTypeCode, dto.OwnershipTypeCode, dto.LogoUrl, dto.WebsiteUrl, dto.EstablishedOn, dto.ParentOrganizationId);
            o.HydrateLegal(dto.LegalName, dto.LegalEntityTypeCode, dto.RegistrationNumber, dto.NationalFacilityCode,
                dto.LicenseNumber, dto.LicenseIssuedOn, dto.LicenseExpiresOn, dto.AccreditationStatusCode, dto.TaxIdentificationNumber);
            o.HydrateAddress(dto.CountryCode, dto.RegionCode, dto.PrefectureCode, dto.CommuneCode, dto.Locality,
                dto.DistrictOrNeighborhood, dto.StreetAddress, dto.PostalCode, dto.Latitude, dto.Longitude, dto.AltitudeMeters, dto.LocationAccuracy, dto.TimeZoneId);
            o.HydrateOperations(dto.OpeningDate, dto.DefaultCurrencyCode, dto.DefaultLanguageCode, dto.EmergencyServicesAvailable, dto.PublicFacing,
                Enum.Parse<OperationalStatus>(dto.OperationalStatus), Enum.Parse<ConfigurationStatus>(dto.ConfigurationStatus),
                dto.Phone, dto.Email, new DateTimeOffset(dto.CreatedOnUtc, TimeSpan.Zero),
                dto.UpdatedOnUtc is { } u ? new DateTimeOffset(u, TimeSpan.Zero) : null);
        });
    }

    public async Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter filter, CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("where 1 = 1");
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Append(" and (official_name ilike @search or code ilike @search or display_name ilike @search)");
            p.Add("search", $"%{filter.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.FacilityTypeCode))
        {
            where.Append(" and facility_type_code = @type");
            p.Add("type", filter.FacilityTypeCode.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filter.OperationalStatus))
        {
            where.Append(" and operational_status = @status");
            p.Add("status", filter.OperationalStatus.Trim());
        }
        p.Add("limit", filter.SafePageSize);
        p.Add("offset", filter.Offset);

        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"select count(*) from organization.organizations {where}", p, cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<OrganizationDto>(new CommandDefinition(
            $"select {Columns} from organization.organizations {where} order by official_name limit @limit offset @offset",
            p, cancellationToken: cancellationToken))).ToList();

        return new PagedResult<OrganizationDto>(items, filter.SafePage, filter.SafePageSize, total);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists(select 1 from organization.organizations where lower(code) = lower(@code) and (@excludeId is null or id <> @excludeId))";
        var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { code, excludeId }, cancellationToken: cancellationToken));
    }

    private static object ToParams(DomainOrganization o) => new
    {
        o.Id, o.Code, o.OfficialName, o.DisplayName, o.Abbreviation, o.Description, o.FacilityTypeCode, o.OwnershipTypeCode,
        o.LogoUrl, o.WebsiteUrl, o.EstablishedOn, o.ParentOrganizationId,
        o.LegalName, o.LegalEntityTypeCode, o.RegistrationNumber, o.NationalFacilityCode, o.LicenseNumber,
        o.LicenseIssuedOn, o.LicenseExpiresOn, o.AccreditationStatusCode, o.TaxIdentificationNumber,
        o.CountryCode, o.RegionCode, o.PrefectureCode, o.CommuneCode, o.Locality, o.DistrictOrNeighborhood, o.StreetAddress, o.PostalCode,
        o.Latitude, o.Longitude, o.AltitudeMeters, o.LocationAccuracy, o.TimeZoneId,
        o.OpeningDate, o.DefaultCurrencyCode, o.DefaultLanguageCode, o.EmergencyServicesAvailable, o.PublicFacing,
        OperationalStatus = o.OperationalStatus.ToString(), ConfigurationStatus = o.ConfigurationStatus.ToString(),
        o.Phone, o.Email, o.CreatedOnUtc, o.UpdatedOnUtc,
    };
}
