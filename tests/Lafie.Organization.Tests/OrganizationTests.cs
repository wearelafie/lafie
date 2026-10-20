using Lafie.Organization.Domain;
using Lafie.Organization.Domain.Events;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Tests;

public class OrganizationTests
{
    [Fact]
    public void Create_valid_returns_success_and_raises_event()
    {
        var result = DomainOrganization.Create("  CHU Sylvanus Olympio  ", OrganizationType.Hospital, " CHU-SO ");

        Assert.True(result.IsSuccess);
        var org = result.Value;
        Assert.Equal("CHU Sylvanus Olympio", org.Name); // trim
        Assert.Equal("CHU-SO", org.Code);
        Assert.True(org.Active);
        Assert.NotEqual(Guid.Empty, org.Id);
        Assert.Single(org.DomainEvents);
        var evt = Assert.IsType<OrganizationCreated>(org.DomainEvents.First());
        Assert.Equal(org.Id, evt.OrganizationId);
        Assert.Equal(OrganizationType.Hospital, evt.Type);
    }

    [Fact]
    public void Create_blank_name_fails()
    {
        var result = DomainOrganization.Create("   ", OrganizationType.Clinic);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_too_long_name_fails()
    {
        var result = DomainOrganization.Create(new string('a', DomainOrganization.NameMaxLength + 1), OrganizationType.Unit);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_empty_code_is_normalized_to_null()
    {
        var org = DomainOrganization.Create("Clinique Biasa", OrganizationType.Clinic, "   ").Value;
        Assert.Null(org.Code);
    }

    [Fact]
    public void Deactivate_sets_inactive()
    {
        var org = DomainOrganization.Create("Service X", OrganizationType.Department).Value;
        org.Deactivate();
        Assert.False(org.Active);
    }
}
