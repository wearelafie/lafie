using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Lafie.Organization.Infrastructure;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.IntegrationTests;

public class SpaceRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private SpaceRepository Spaces() => new(fixture.ConnectionFactory);

    private sealed record Lineage(Guid FacilityId, Guid BuildingId, Guid FloorId);

    private async Task<Lineage> NewFloorAsync()
    {
        var facility = DomainOrganization.Create($"F{Guid.NewGuid():N}", OrganizationType.Hospital).Value;
        await new OrganizationRepository(fixture.ConnectionFactory).AddAsync(facility);
        var building = Building.Create(facility.Id, $"B{Guid.NewGuid():N}", "Bât").Value;
        await new BuildingRepository(fixture.ConnectionFactory).AddAsync(building);
        var floor = Floor.Create(building.Id, $"FL{Guid.NewGuid():N}", "RDC", 0).Value;
        await new FloorRepository(fixture.ConnectionFactory).AddAsync(floor);
        return new Lineage(facility.Id, building.Id, floor.Id);
    }

    [Fact]
    public async Task Add_then_GetById_roundtrips_metrics()
    {
        var repo = Spaces();
        var l = await NewFloorAsync();
        var s = Space.Create(l.FacilityId, l.BuildingId, l.FloorId, "CH-101", "Chambre 101", SpaceType.PatientRoom, "std", 18.5m, 2, true).Value;
        await repo.AddAsync(s);

        var dto = await repo.GetByIdAsync(s.Id);
        Assert.NotNull(dto);
        Assert.Equal("CH-101", dto!.Code);
        Assert.Equal("PatientRoom", dto.Type);
        Assert.Equal(18.5m, dto.Area);
        Assert.Equal(2, dto.MaxCapacity);
        Assert.True(dto.Accessible);
    }

    [Fact]
    public async Task CodeExists_scoped_to_facility()
    {
        var repo = Spaces();
        var l = await NewFloorAsync();
        await repo.AddAsync(Space.Create(l.FacilityId, l.BuildingId, l.FloorId, "DUP", "N", SpaceType.Office).Value);

        Assert.True(await repo.CodeExistsAsync(l.FacilityId, "dup"));
        var other = await NewFloorAsync();
        Assert.False(await repo.CodeExistsAsync(other.FacilityId, "DUP"));
    }

    [Fact]
    public async Task Update_and_SetStatus_persist()
    {
        var repo = Spaces();
        var l = await NewFloorAsync();
        var s = Space.Create(l.FacilityId, l.BuildingId, l.FloorId, "C1", "Avant", SpaceType.Office).Value;
        await repo.AddAsync(s);

        var loaded = await repo.FindAsync(s.Id);
        loaded!.Update("C2", "Après", SpaceType.ConsultationRoom, null, 10m, 1, true);
        loaded.SetStatus(SpaceStatus.Maintenance);
        await repo.UpdateAsync(loaded);

        var dto = await repo.GetByIdAsync(s.Id);
        Assert.Equal("ConsultationRoom", dto!.Type);
        Assert.Equal(10m, dto.Area);
        Assert.Equal("Maintenance", dto.Status);
    }

    [Fact]
    public async Task Search_filters_by_floor_type_and_status()
    {
        var repo = Spaces();
        var l = await NewFloorAsync();
        await repo.AddAsync(Space.Create(l.FacilityId, l.BuildingId, l.FloorId, "R1", "Chambre", SpaceType.PatientRoom).Value);
        await repo.AddAsync(Space.Create(l.FacilityId, l.BuildingId, l.FloorId, "O1", "Bureau", SpaceType.Office).Value);

        var all = await repo.SearchByFloorAsync(l.FloorId, new SpaceFilter());
        Assert.Equal(2, all.Total);

        var rooms = await repo.SearchByFloorAsync(l.FloorId, new SpaceFilter(Type: "PatientRoom"));
        Assert.Equal(1, rooms.Total);
        Assert.Equal("R1", rooms.Items[0].Code);
    }
}
