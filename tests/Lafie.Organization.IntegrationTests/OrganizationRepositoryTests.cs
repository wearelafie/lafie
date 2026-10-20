using Lafie.Organization.Application;
using Lafie.Organization.Domain;
using Lafie.Organization.Infrastructure;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.IntegrationTests;

public class OrganizationRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private OrganizationRepository NewRepo() => new(fixture.ConnectionFactory);

    [Fact]
    public async Task Add_then_GetById_roundtrips()
    {
        var repo = NewRepo();
        var org = DomainOrganization.Create(
            "CHU Test", OrganizationType.Hospital, "CHU-T",
            new OrganizationProfile(ShortName: "CHU", City: "Lomé", Country: "Togo")).Value;
        await repo.AddAsync(org);

        var dto = await repo.GetByIdAsync(org.Id);

        Assert.NotNull(dto);
        Assert.Equal("CHU Test", dto!.Name);
        Assert.Equal("Hospital", dto.Type);
        Assert.Equal("Active", dto.Status);
        Assert.Equal("CHU", dto.ShortName);
        Assert.Equal("Togo", dto.Country);
    }

    [Fact]
    public async Task Update_persists_changes()
    {
        var repo = NewRepo();
        var org = DomainOrganization.Create("Avant", OrganizationType.Clinic).Value;
        await repo.AddAsync(org);

        var loaded = await repo.FindAsync(org.Id);
        Assert.NotNull(loaded);
        loaded!.Update("Après", new OrganizationProfile(Phone: "90000000"));
        await repo.UpdateAsync(loaded);

        var dto = await repo.GetByIdAsync(org.Id);
        Assert.Equal("Après", dto!.Name);
        Assert.Equal("90000000", dto.Phone);
        Assert.NotNull(dto.UpdatedOnUtc);
    }

    [Fact]
    public async Task Deactivate_persists_status()
    {
        var repo = NewRepo();
        var org = DomainOrganization.Create("À désactiver", OrganizationType.Unit).Value;
        await repo.AddAsync(org);

        var loaded = await repo.FindAsync(org.Id);
        loaded!.Deactivate();
        await repo.UpdateAsync(loaded);

        var dto = await repo.GetByIdAsync(org.Id);
        Assert.Equal("Inactive", dto!.Status);
    }

    [Fact]
    public async Task Search_filters_by_status_and_search_term()
    {
        var repo = NewRepo();
        var marker = $"Z{Guid.NewGuid():N}";
        var a = DomainOrganization.Create($"{marker} Active", OrganizationType.Hospital).Value;
        var b = DomainOrganization.Create($"{marker} Inactive", OrganizationType.Hospital).Value;
        b.Deactivate();
        await repo.AddAsync(a);
        await repo.AddAsync(b);

        var active = await repo.SearchAsync(new OrganizationFilter(Search: marker, Status: "Active"));
        Assert.Equal(1, active.Total);
        Assert.Equal($"{marker} Active", active.Items[0].Name);

        var all = await repo.SearchAsync(new OrganizationFilter(Search: marker));
        Assert.Equal(2, all.Total);
    }

    [Fact]
    public async Task Search_paginates()
    {
        var repo = NewRepo();
        var marker = $"P{Guid.NewGuid():N}";
        for (var i = 0; i < 3; i++)
            await repo.AddAsync(DomainOrganization.Create($"{marker} {i}", OrganizationType.Department).Value);

        var page1 = await repo.SearchAsync(new OrganizationFilter(Search: marker, Page: 1, PageSize: 2));
        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page1.TotalPages);

        var page2 = await repo.SearchAsync(new OrganizationFilter(Search: marker, Page: 2, PageSize: 2));
        Assert.Single(page2.Items);
    }
}
