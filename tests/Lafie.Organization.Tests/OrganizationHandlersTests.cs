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

        public Task AddAsync(DomainOrganization o, CancellationToken ct = default)
        {
            Store.Add(o);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DomainOrganization o, CancellationToken ct = default) => Task.CompletedTask;

        public Task<DomainOrganization?> FindAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(x => x.Id == id));

        public Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter f, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<OrganizationDto>([], f.SafePage, f.SafePageSize, 0));

        public Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<OrganizationDto?>(null);
    }

    [Fact]
    public async Task Create_valid_persists_and_returns_id()
    {
        var repo = new FakeRepository();
        var result = await new CreateOrganizationHandler(repo)
            .Handle(new CreateOrganizationCommand("CHU", "hospital", "CHU-SO"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Assert.Single(repo.Store).Id, result.Value);
    }

    [Fact]
    public async Task Create_unknown_type_fails_and_persists_nothing()
    {
        var repo = new FakeRepository();
        var result = await new CreateOrganizationHandler(repo)
            .Handle(new CreateOrganizationCommand("X", "Spaceship"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repo.Store);
    }

    [Fact]
    public async Task Update_existing_succeeds()
    {
        var repo = new FakeRepository();
        var org = DomainOrganization.Create("Old", Lafie.Organization.Domain.OrganizationType.Clinic).Value;
        repo.Store.Add(org);

        var result = await new UpdateOrganizationHandler(repo)
            .Handle(new UpdateOrganizationCommand(org.Id, "New"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", org.Name);
    }

    [Fact]
    public async Task Update_missing_throws_not_found()
    {
        var repo = new FakeRepository();
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateOrganizationHandler(repo)
            .Handle(new UpdateOrganizationCommand(Guid.NewGuid(), "X"), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Deactivate_existing_sets_inactive()
    {
        var repo = new FakeRepository();
        var org = DomainOrganization.Create("Svc", Lafie.Organization.Domain.OrganizationType.Department).Value;
        repo.Store.Add(org);

        var result = await new DeactivateOrganizationHandler(repo)
            .Handle(new DeactivateOrganizationCommand(org.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Lafie.Organization.Domain.OrganizationStatus.Inactive, org.Status);
    }
}
