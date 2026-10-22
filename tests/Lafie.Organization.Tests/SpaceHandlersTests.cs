using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Application.Floors;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Tests;

public class SpaceHandlersTests
{
    private static readonly Guid FloorId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.NewGuid();
    private static readonly Guid FacilityId = Guid.NewGuid();

    private sealed class FakeSpaces : ISpaceRepository
    {
        public List<Space> Store { get; } = [];
        public HashSet<string> ExistingCodes { get; } = [];
        public Task AddAsync(Space s, CancellationToken ct = default) { Store.Add(s); return Task.CompletedTask; }
        public Task UpdateAsync(Space s, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Space?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Store.FirstOrDefault(x => x.Id == id));
        public Task<SpaceDto?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<SpaceDto?>(null);
        public Task<PagedResult<SpaceDto>> SearchByFloorAsync(Guid f, SpaceFilter filter, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<SpaceDto>([], filter.SafePage, filter.SafePageSize, 0));
        public Task<bool> CodeExistsAsync(Guid f, string code, Guid? excludeId = null, CancellationToken ct = default)
            => Task.FromResult(ExistingCodes.Contains(code));
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeFloors(bool exists) : IFloorRepository
    {
        public Task AddAsync(Floor f, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Floor f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Floor?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Floor?>(null);
        public Task<FloorDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(exists ? new FloorDto { Id = id, BuildingId = BuildingId, Code = "F", Name = "F", Status = "Active" } : null);
        public Task<PagedResult<FloorDto>> SearchAsync(Guid b, FloorFilter filter, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<FloorDto>([], 1, 20, 0));
        public Task<bool> CodeExistsAsync(Guid b, string code, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBuildings : IBuildingRepository
    {
        public Task AddAsync(Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Building?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Building?>(null);
        public Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<BuildingDto?>(new BuildingDto { Id = id, FacilityId = FacilityId, Code = "B", Name = "B", Status = "Active" });
        public Task<PagedResult<BuildingDto>> SearchAsync(Guid f, BuildingFilter filter, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<BuildingDto>([], 1, 20, 0));
        public Task<bool> CodeExistsAsync(Guid f, string code, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static CreateSpaceHandler Handler(FakeSpaces spaces, bool floorExists = true)
        => new(spaces, new FakeFloors(floorExists), new FakeBuildings());

    [Fact]
    public async Task Create_valid_resolves_lineage_and_persists()
    {
        var spaces = new FakeSpaces();
        var r = await Handler(spaces).Handle(new CreateSpaceCommand(FloorId, "CH-101", "Chambre 101", "PatientRoom"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        var s = Assert.Single(spaces.Store);
        Assert.Equal(FacilityId, s.FacilityId);
        Assert.Equal(BuildingId, s.BuildingId);
        Assert.Equal(FloorId, s.FloorId);
    }

    [Fact]
    public async Task Create_unknown_floor_fails()
    {
        var spaces = new FakeSpaces();
        var r = await Handler(spaces, floorExists: false).Handle(new CreateSpaceCommand(FloorId, "C", "N", "Office"), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.Empty(spaces.Store);
    }

    [Fact]
    public async Task Create_unknown_type_fails()
    {
        var spaces = new FakeSpaces();
        var r = await Handler(spaces).Handle(new CreateSpaceCommand(FloorId, "C", "N", "Dungeon"), CancellationToken.None);
        Assert.True(r.IsFailure);
    }

    [Fact]
    public async Task Create_duplicate_code_fails()
    {
        var spaces = new FakeSpaces();
        spaces.ExistingCodes.Add("CH-101");
        var r = await Handler(spaces).Handle(new CreateSpaceCommand(FloorId, "CH-101", "N", "PatientRoom"), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.Empty(spaces.Store);
    }

    [Fact]
    public async Task Update_missing_throws_not_found()
    {
        var spaces = new FakeSpaces();
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateSpaceHandler(spaces)
            .Handle(new UpdateSpaceCommand(Guid.NewGuid(), "C", "N", "Office"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetStatus_valid_updates()
    {
        var spaces = new FakeSpaces();
        var s = Space.Create(FacilityId, BuildingId, FloorId, "C", "N", SpaceType.Office).Value;
        spaces.Store.Add(s);
        var r = await new SetSpaceStatusHandler(spaces).Handle(new SetSpaceStatusCommand(s.Id, "maintenance"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(SpaceStatus.Maintenance, s.Status);
    }
}
