using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Domain;
using Lafie.Organization.Infrastructure;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.IntegrationTests;

public class BuildingRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private BuildingRepository Buildings() => new(fixture.ConnectionFactory);
    private OrganizationRepository Facilities() => new(fixture.ConnectionFactory);

    private async Task<Guid> NewFacilityAsync()
    {
        var facility = DomainOrganization.Create($"F{Guid.NewGuid():N}", "Facility", "HOSPITAL", "PUBLIC", "TG").Value;
        await Facilities().AddAsync(facility);
        return facility.Id;
    }

    [Fact]
    public async Task Add_then_GetById_roundtrips()
    {
        var repo = Buildings();
        var facilityId = await NewFacilityAsync();
        var b = Building.Create(facilityId, "BAT-A", "Bâtiment A", "desc", "aile est").Value;
        await repo.AddAsync(b);

        var dto = await repo.GetByIdAsync(b.Id);
        Assert.NotNull(dto);
        Assert.Equal("BAT-A", dto!.Code);
        Assert.Equal(facilityId, dto.FacilityId);
        Assert.Equal("Active", dto.Status);
    }

    [Fact]
    public async Task CodeExists_detects_duplicate_within_facility()
    {
        var repo = Buildings();
        var facilityId = await NewFacilityAsync();
        await repo.AddAsync(Building.Create(facilityId, "DUP", "B").Value);

        Assert.True(await repo.CodeExistsAsync(facilityId, "dup"));          // insensible à la casse
        Assert.False(await repo.CodeExistsAsync(facilityId, "OTHER"));
        Assert.False(await repo.CodeExistsAsync(await NewFacilityAsync(), "DUP")); // autre établissement
    }

    [Fact]
    public async Task Update_and_SetStatus_persist()
    {
        var repo = Buildings();
        var facilityId = await NewFacilityAsync();
        var b = Building.Create(facilityId, "C1", "Avant").Value;
        await repo.AddAsync(b);

        var loaded = await repo.FindAsync(b.Id);
        loaded!.Update("C2", "Après", null, null);
        loaded.SetStatus(BuildingStatus.Maintenance);
        await repo.UpdateAsync(loaded);

        var dto = await repo.GetByIdAsync(b.Id);
        Assert.Equal("C2", dto!.Code);
        Assert.Equal("Après", dto.Name);
        Assert.Equal("Maintenance", dto.Status);
        Assert.NotNull(dto.UpdatedOnUtc);
    }

    [Fact]
    public async Task Search_scopes_to_facility_and_filters()
    {
        var repo = Buildings();
        var f1 = await NewFacilityAsync();
        var f2 = await NewFacilityAsync();
        await repo.AddAsync(Building.Create(f1, "A1", "Alpha").Value);
        await repo.AddAsync(Building.Create(f1, "A2", "Beta").Value);
        await repo.AddAsync(Building.Create(f2, "B1", "Gamma").Value);

        var all = await repo.SearchAsync(f1, new BuildingFilter());
        Assert.Equal(2, all.Total);

        var filtered = await repo.SearchAsync(f1, new BuildingFilter(Search: "Alpha"));
        Assert.Equal(1, filtered.Total);
        Assert.Equal("A1", filtered.Items[0].Code);
    }
}
