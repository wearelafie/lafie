using Lafie.Core.Common;
using Lafie.Organization.Application;
using Lafie.Organization.Application.Buildings;
using Lafie.Organization.Application.Organizations;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Tests;

public class OrganizationLifecycleTests
{
    private sealed class FakeOrgs : IOrganizationRepository
    {
        public List<DomainOrganization> Store { get; } = [];
        public bool Deleted { get; private set; }
        public Task AddAsync(DomainOrganization o, CancellationToken ct = default) { Store.Add(o); return Task.CompletedTask; }
        public Task UpdateAsync(DomainOrganization o, CancellationToken ct = default) => Task.CompletedTask;
        public Task<DomainOrganization?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Store.FirstOrDefault(x => x.Id == id));
        public Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter f, CancellationToken ct = default) => Task.FromResult(new PagedResult<OrganizationDto>([], 1, 20, 0));
        public Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.Any(x => x.Id == id) ? new OrganizationDto { Id = id } : null);
        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) { Deleted = true; return Task.CompletedTask; }
    }

    private sealed class FakeBuildings(long total) : IBuildingRepository
    {
        public Task AddAsync(Lafie.Organization.Domain.Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Lafie.Organization.Domain.Building b, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Lafie.Organization.Domain.Building?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Lafie.Organization.Domain.Building?>(null);
        public Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<BuildingDto?>(null);
        public Task<PagedResult<BuildingDto>> SearchAsync(Guid f, BuildingFilter filter, CancellationToken ct = default) => Task.FromResult(new PagedResult<BuildingDto>([], 1, 1, total));
        public Task<bool> CodeExistsAsync(Guid f, string code, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static DomainOrganization New() => DomainOrganization.Create("C", "N", "HOSPITAL", "PUBLIC", "TG").Value;

    [Fact]
    public async Task Archive_then_restore_toggles_flag()
    {
        var repo = new FakeOrgs();
        var o = New(); repo.Store.Add(o);
        await new ArchiveOrganizationHandler(repo).Handle(new ArchiveOrganizationCommand(o.Id), CancellationToken.None);
        Assert.True(o.IsArchived);
        await new RestoreOrganizationHandler(repo).Handle(new RestoreOrganizationCommand(o.Id), CancellationToken.None);
        Assert.False(o.IsArchived);
    }

    [Fact]
    public async Task Delete_blocked_when_has_buildings()
    {
        var repo = new FakeOrgs();
        var o = New(); repo.Store.Add(o);
        var r = await new DeleteOrganizationHandler(repo, new FakeBuildings(total: 2)).Handle(new DeleteOrganizationCommand(o.Id), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.False(repo.Deleted);
    }

    [Fact]
    public async Task Delete_ok_when_empty()
    {
        var repo = new FakeOrgs();
        var o = New(); repo.Store.Add(o);
        var r = await new DeleteOrganizationHandler(repo, new FakeBuildings(total: 0)).Handle(new DeleteOrganizationCommand(o.Id), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.True(repo.Deleted);
    }
}
