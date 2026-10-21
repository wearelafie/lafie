using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Tests;

public class BuildingHandlersTests
{
    private sealed class FakeBuildings : IBuildingRepository
    {
        public List<Building> Store { get; } = [];
        public HashSet<string> ExistingCodes { get; } = [];

        public Task AddAsync(Building b, CancellationToken ct = default) { Store.Add(b); return Task.CompletedTask; }
        public Task UpdateAsync(Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Building?> FindAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(x => x.Id == id));
        public Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<BuildingDto?>(null);
        public Task<PagedResult<BuildingDto>> SearchAsync(Guid f, BuildingFilter filter, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<BuildingDto>([], filter.SafePage, filter.SafePageSize, 0));
        public Task<bool> CodeExistsAsync(Guid f, string code, Guid? excludeId = null, CancellationToken ct = default)
            => Task.FromResult(ExistingCodes.Contains(code));
    }

    // Établissement : on simule l'existence via GetByIdAsync.
    private sealed class FakeFacilities(bool exists) : IOrganizationRepository
    {
        public Task AddAsync(Lafie.Organization.Domain.Organization o, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Lafie.Organization.Domain.Organization o, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Lafie.Organization.Domain.Organization?> FindAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Lafie.Organization.Domain.Organization?>(null);
        public Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter f, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<OrganizationDto>([], 1, 20, 0));
        public Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(exists ? new OrganizationDto { Id = id, Code = "F", OfficialName = "F", FacilityTypeCode = "HOSPITAL", OwnershipTypeCode = "PUBLIC", CountryCode = "TG", OperationalStatus = "Active", ConfigurationStatus = "Draft", TimeZoneId = "Africa/Lome" } : null);
        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
    }

    [Fact]
    public async Task Create_valid_persists()
    {
        var buildings = new FakeBuildings();
        var handler = new CreateBuildingHandler(buildings, new FakeFacilities(exists: true));
        var result = await handler.Handle(new CreateBuildingCommand(Guid.NewGuid(), "BAT-A", "Bâtiment A"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(buildings.Store);
    }

    [Fact]
    public async Task Create_unknown_facility_fails()
    {
        var buildings = new FakeBuildings();
        var handler = new CreateBuildingHandler(buildings, new FakeFacilities(exists: false));
        var result = await handler.Handle(new CreateBuildingCommand(Guid.NewGuid(), "BAT-A", "A"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(buildings.Store);
    }

    [Fact]
    public async Task Create_duplicate_code_fails()
    {
        var buildings = new FakeBuildings();
        buildings.ExistingCodes.Add("BAT-A");
        var handler = new CreateBuildingHandler(buildings, new FakeFacilities(exists: true));
        var result = await handler.Handle(new CreateBuildingCommand(Guid.NewGuid(), "BAT-A", "A"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(buildings.Store);
    }

    [Fact]
    public async Task Update_missing_throws_not_found()
    {
        var buildings = new FakeBuildings();
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateBuildingHandler(buildings)
            .Handle(new UpdateBuildingCommand(Guid.NewGuid(), "C", "N"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetStatus_invalid_fails()
    {
        var buildings = new FakeBuildings();
        var b = Building.Create(Guid.NewGuid(), "C", "N").Value;
        buildings.Store.Add(b);
        var result = await new SetBuildingStatusHandler(buildings)
            .Handle(new SetBuildingStatusCommand(b.Id, "Flying"), CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task SetStatus_valid_updates()
    {
        var buildings = new FakeBuildings();
        var b = Building.Create(Guid.NewGuid(), "C", "N").Value;
        buildings.Store.Add(b);
        var result = await new SetBuildingStatusHandler(buildings)
            .Handle(new SetBuildingStatusCommand(b.Id, "maintenance"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(BuildingStatus.Maintenance, b.Status);
    }
}
