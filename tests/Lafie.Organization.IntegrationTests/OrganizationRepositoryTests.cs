using Lafie.Organization.Application;
using Lafie.Organization.Domain;
using Lafie.Organization.Infrastructure;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.IntegrationTests;

public class OrganizationRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private OrganizationRepository NewRepo() => new(fixture.ConnectionFactory);

    private static DomainOrganization Facility(string code, string name, string type = "HOSPITAL",
        FacilityAddress? address = null, FacilityLegal? legal = null)
        => DomainOrganization.Create(code, name, type, "PUBLIC", "TG", address: address, legal: legal).Value;

    [Fact]
    public async Task Add_then_GetById_roundtrips_rich_fields()
    {
        var repo = NewRepo();
        var o = Facility($"CHU-{Guid.NewGuid():N}", "CHU Test",
            address: new FacilityAddress(Locality: "Lomé", Latitude: 6.13m, Longitude: 1.22m, TimeZoneId: "Africa/Lome"),
            legal: new FacilityLegal(LicenseNumber: "LIC-1", LicenseIssuedOn: new DateOnly(2024, 1, 1)));
        await repo.AddAsync(o);

        var dto = await repo.GetByIdAsync(o.Id);
        Assert.NotNull(dto);
        Assert.Equal("CHU Test", dto!.OfficialName);
        Assert.Equal("HOSPITAL", dto.FacilityTypeCode);
        Assert.Equal("Active", dto.OperationalStatus);
        Assert.Equal("Lomé", dto.Locality);
        Assert.Equal(6.13m, dto.Latitude);
        Assert.Equal("LIC-1", dto.LicenseNumber);
        Assert.Equal(new DateOnly(2024, 1, 1), dto.LicenseIssuedOn);
    }

    [Fact]
    public async Task Update_and_ChangeStatus_persist()
    {
        var repo = NewRepo();
        var o = Facility($"F-{Guid.NewGuid():N}", "Avant");
        await repo.AddAsync(o);

        var loaded = await repo.FindAsync(o.Id);
        Assert.NotNull(loaded);
        loaded!.Update(loaded.Code, "Après", "CLINIC", "PRIVATE_FOR_PROFIT", "TG",
            new FacilityIdentity(DisplayName: "AP"), new FacilityAddress(Locality: "Kara"),
            new FacilityLegal(), new FacilityOperations(EmergencyServicesAvailable: true), new FacilityPrimaryContact(Email: "x@y.tg"));
        loaded.ChangeOperationalStatus(OperationalStatus.Suspended);
        await repo.UpdateAsync(loaded);

        var dto = await repo.GetByIdAsync(o.Id);
        Assert.Equal("Après", dto!.OfficialName);
        Assert.Equal("CLINIC", dto.FacilityTypeCode);
        Assert.Equal("Kara", dto.Locality);
        Assert.True(dto.EmergencyServicesAvailable);
        Assert.Equal("Suspended", dto.OperationalStatus);
    }

    [Fact]
    public async Task CodeExists_is_case_insensitive_and_scoped()
    {
        var repo = NewRepo();
        var code = $"DUP-{Guid.NewGuid():N}";
        await repo.AddAsync(Facility(code, "A"));

        Assert.True(await repo.CodeExistsAsync(code.ToLowerInvariant()));
        Assert.False(await repo.CodeExistsAsync($"OTHER-{Guid.NewGuid():N}"));
    }

    [Fact]
    public async Task Search_filters_by_type_and_search()
    {
        var repo = NewRepo();
        var marker = $"Z{Guid.NewGuid():N}";
        await repo.AddAsync(Facility($"{marker}-H", $"{marker} Hosp", "HOSPITAL"));
        await repo.AddAsync(Facility($"{marker}-C", $"{marker} Clin", "CLINIC"));

        var all = await repo.SearchAsync(new OrganizationFilter(Search: marker));
        Assert.Equal(2, all.Total);

        var hosp = await repo.SearchAsync(new OrganizationFilter(Search: marker, FacilityTypeCode: "HOSPITAL"));
        Assert.Equal(1, hosp.Total);
        Assert.Equal($"{marker} Hosp", hosp.Items[0].OfficialName);
    }
}
