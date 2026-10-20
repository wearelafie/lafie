using Lafie.Organization.Application.Floors;
using Lafie.Organization.Domain;
using Lafie.Organization.Infrastructure;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.IntegrationTests;

public class FloorRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private FloorRepository Floors() => new(fixture.ConnectionFactory);

    private async Task<Guid> NewBuildingAsync()
    {
        var facility = DomainOrganization.Create($"F{Guid.NewGuid():N}", OrganizationType.Hospital).Value;
        await new OrganizationRepository(fixture.ConnectionFactory).AddAsync(facility);
        var building = Building.Create(facility.Id, $"B{Guid.NewGuid():N}", "Bât").Value;
        await new BuildingRepository(fixture.ConnectionFactory).AddAsync(building);
        return building.Id;
    }

    [Fact]
    public async Task Add_then_GetById_roundtrips_with_negative_level()
    {
        var repo = Floors();
        var buildingId = await NewBuildingAsync();
        var f = Floor.Create(buildingId, "SS1", "Sous-sol 1", -1, 1).Value;
        await repo.AddAsync(f);

        var dto = await repo.GetByIdAsync(f.Id);
        Assert.NotNull(dto);
        Assert.Equal("SS1", dto!.Code);
        Assert.Equal(-1, dto.Level);
        Assert.Equal(buildingId, dto.BuildingId);
    }

    [Fact]
    public async Task CodeExists_scoped_to_building()
    {
        var repo = Floors();
        var buildingId = await NewBuildingAsync();
        await repo.AddAsync(Floor.Create(buildingId, "RDC", "Rez").Value);

        Assert.True(await repo.CodeExistsAsync(buildingId, "rdc"));
        Assert.False(await repo.CodeExistsAsync(await NewBuildingAsync(), "RDC"));
    }

    [Fact]
    public async Task Update_and_SetStatus_persist()
    {
        var repo = Floors();
        var buildingId = await NewBuildingAsync();
        var f = Floor.Create(buildingId, "C1", "Avant", 0).Value;
        await repo.AddAsync(f);

        var loaded = await repo.FindAsync(f.Id);
        loaded!.Update("C2", "Après", 3, 9, null);
        loaded.SetStatus(FloorStatus.Maintenance);
        await repo.UpdateAsync(loaded);

        var dto = await repo.GetByIdAsync(f.Id);
        Assert.Equal("C2", dto!.Code);
        Assert.Equal(3, dto.Level);
        Assert.Equal("Maintenance", dto.Status);
    }

    [Fact]
    public async Task Search_orders_by_display_order()
    {
        var repo = Floors();
        var buildingId = await NewBuildingAsync();
        await repo.AddAsync(Floor.Create(buildingId, "F2", "Deux", 2, displayOrder: 2).Value);
        await repo.AddAsync(Floor.Create(buildingId, "F1", "Un", 1, displayOrder: 1).Value);

        var page = await repo.SearchAsync(buildingId, new FloorFilter());
        Assert.Equal(2, page.Total);
        Assert.Equal("F1", page.Items[0].Code); // display_order 1 d'abord
    }
}
