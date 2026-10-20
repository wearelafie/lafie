using Lafie.Organization.Application;
using Lafie.Organization.Application.Organizations;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Tests;

public class CreateOrganizationHandlerTests
{
    private sealed class FakeRepository : IOrganizationRepository
    {
        public List<DomainOrganization> Added { get; } = [];

        public Task AddAsync(DomainOrganization organization, CancellationToken cancellationToken = default)
        {
            Added.Add(organization);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OrganizationDto>>([]);

        public Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<OrganizationDto?>(null);
    }

    [Fact]
    public async Task Handle_valid_persists_and_returns_id()
    {
        var repo = new FakeRepository();
        var handler = new CreateOrganizationHandler(repo);

        var result = await handler.Handle(
            new CreateOrganizationCommand("CHU", "hospital", "CHU-SO"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var org = Assert.Single(repo.Added);
        Assert.Equal(org.Id, result.Value);
        Assert.Equal("CHU", org.Name);
    }

    [Fact]
    public async Task Handle_unknown_type_returns_failure_and_persists_nothing()
    {
        var repo = new FakeRepository();
        var handler = new CreateOrganizationHandler(repo);

        var result = await handler.Handle(
            new CreateOrganizationCommand("X", "Spaceship"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repo.Added);
    }

    [Fact]
    public async Task Handle_blank_name_returns_failure()
    {
        var repo = new FakeRepository();
        var handler = new CreateOrganizationHandler(repo);

        var result = await handler.Handle(
            new CreateOrganizationCommand("  ", "Clinic"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(repo.Added);
    }
}
