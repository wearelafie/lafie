using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application;
using Lafie.Organization.Application.Organizations;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Tests;

public class OrganizationHandlersTests
{
    private sealed class FakeRepository : IOrganizationRepository
    {
        public List<DomainOrganization> Store { get; } = [];
        public HashSet<string> ExistingCodes { get; } = [];

        public Task AddAsync(DomainOrganization o, CancellationToken ct = default) { Store.Add(o); return Task.CompletedTask; }
        public Task UpdateAsync(DomainOrganization o, CancellationToken ct = default) => Task.CompletedTask;
        public Task<DomainOrganization?> FindAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(x => x.Id == id));
        public Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter f, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<OrganizationDto>([], f.SafePage, f.SafePageSize, 0));
        public Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<OrganizationDto?>(null);
        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
            => Task.FromResult(ExistingCodes.Contains(code));
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static OrganizationWriteModel Model(string code = "CHU", string name = "CHU") =>
        new(code, name, "HOSPITAL", "PUBLIC", "TG");

    [Fact]
    public async Task Create_valid_persists_and_returns_id()
    {
        var repo = new FakeRepository();
        var r = await new CreateOrganizationHandler(repo).Handle(new CreateOrganizationCommand(Model()), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(Assert.Single(repo.Store).Id, r.Value);
    }

    [Fact]
    public async Task Create_invalid_country_fails()
    {
        var repo = new FakeRepository();
        var r = await new CreateOrganizationHandler(repo)
            .Handle(new CreateOrganizationCommand(Model() with { CountryCode = "TGO" }), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.Empty(repo.Store);
    }

    [Fact]
    public async Task Create_duplicate_code_fails()
    {
        var repo = new FakeRepository();
        repo.ExistingCodes.Add("CHU");
        var r = await new CreateOrganizationHandler(repo).Handle(new CreateOrganizationCommand(Model()), CancellationToken.None);
        Assert.True(r.IsFailure);
        Assert.Empty(repo.Store);
    }

    [Fact]
    public async Task Update_existing_succeeds()
    {
        var repo = new FakeRepository();
        var o = DomainOrganization.Create("OLD", "Old", "HOSPITAL", "PUBLIC", "TG").Value;
        repo.Store.Add(o);
        var r = await new UpdateOrganizationHandler(repo)
            .Handle(new UpdateOrganizationCommand(o.Id, Model("OLD", "New")), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("New", o.OfficialName);
    }

    [Fact]
    public async Task Update_missing_throws_not_found()
    {
        var repo = new FakeRepository();
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateOrganizationHandler(repo)
            .Handle(new UpdateOrganizationCommand(Guid.NewGuid(), Model()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ChangeStatus_valid_updates()
    {
        var repo = new FakeRepository();
        var o = DomainOrganization.Create("C", "N", "HOSPITAL", "PUBLIC", "TG").Value;
        repo.Store.Add(o);
        var r = await new ChangeOrganizationStatusHandler(repo)
            .Handle(new ChangeOrganizationStatusCommand(o.Id, "Suspended"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(Lafie.Organization.Domain.OperationalStatus.Suspended, o.OperationalStatus);
    }

    [Fact]
    public async Task ChangeStatus_invalid_fails()
    {
        var repo = new FakeRepository();
        var o = DomainOrganization.Create("C", "N", "HOSPITAL", "PUBLIC", "TG").Value;
        repo.Store.Add(o);
        var r = await new ChangeOrganizationStatusHandler(repo)
            .Handle(new ChangeOrganizationStatusCommand(o.Id, "Imploded"), CancellationToken.None);
        Assert.True(r.IsFailure);
    }
}
