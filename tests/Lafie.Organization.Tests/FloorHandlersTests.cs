using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Application.Floors;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Tests;

public class FloorHandlersTests
{
    private sealed class FakeFloors : IFloorRepository
    {
        public List<Floor> Store { get; } = [];
        public HashSet<string> ExistingCodes { get; } = [];
        public Task AddAsync(Floor f, CancellationToken ct = default) { Store.Add(f); return Task.CompletedTask; }
        public Task UpdateAsync(Floor f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Floor?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Store.FirstOrDefault(x => x.Id == id));
        public Task<FloorDto?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<FloorDto?>(null);
        public Task<PagedResult<FloorDto>> SearchAsync(Guid b, FloorFilter f, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<FloorDto>([], f.SafePage, f.SafePageSize, 0));
        public Task<bool> CodeExistsAsync(Guid b, string code, Guid? excludeId = null, CancellationToken ct = default)
            => Task.FromResult(ExistingCodes.Contains(code));
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBuildings(bool exists) : IBuildingRepository
    {
        public Task AddAsync(Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Building?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Building?>(null);
        public Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(exists ? new BuildingDto { Id = id, FacilityId = Guid.NewGuid(), Code = "B", Name = "B", Status = "Active" } : null);
        public Task<PagedResult<BuildingDto>> SearchAsync(Guid f, BuildingFilter filter, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<BuildingDto>([], 1, 20, 0));
        public Task<bool> CodeExistsAsync(Guid f, string code, Guid? excludeId = null, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task Create_valid_persists()
    {
        var floors = new FakeFloors();
        var r = await new CreateFloorHandler(floors, new FakeBuildings(true))
            .Handle(new CreateFloorCommand(Guid.NewGuid(), "RDC", "Rez-de-chaussée", 0), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Single(floors.Store);
    }

    [Fact]
    public async Task Create_unknown_building_fails()
    {
        var floors = new FakeFloors();
        var r = await new CreateFloorHandler(floors, new FakeBuildings(false))
            .Handle(new CreateFloorCommand(Guid.NewGuid(), "RDC", "N"), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.Empty(floors.Store);
    }

    [Fact]
    public async Task Create_duplicate_code_fails()
    {
        var floors = new FakeFloors();
        floors.ExistingCodes.Add("RDC");
        var r = await new CreateFloorHandler(floors, new FakeBuildings(true))
            .Handle(new CreateFloorCommand(Guid.NewGuid(), "RDC", "N"), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.Empty(floors.Store);
    }

    [Fact]
    public async Task Update_missing_throws_not_found()
    {
        var floors = new FakeFloors();
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateFloorHandler(floors)
            .Handle(new UpdateFloorCommand(Guid.NewGuid(), "C", "N"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetStatus_valid_updates()
    {
        var floors = new FakeFloors();
        var f = Floor.Create(Guid.NewGuid(), "C", "N").Value;
        floors.Store.Add(f);
        var r = await new SetFloorStatusHandler(floors)
            .Handle(new SetFloorStatusCommand(f.Id, "maintenance"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(FloorStatus.Maintenance, f.Status);
    }
}
