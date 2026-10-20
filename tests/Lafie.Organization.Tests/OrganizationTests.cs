using Lafie.Organization.Domain;
using Lafie.Organization.Domain.Events;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Tests;

public class OrganizationTests
{
    [Fact]
    public void Create_valid_returns_success_and_raises_event()
    {
        var result = DomainOrganization.Create(
            "  CHU Sylvanus Olympio  ", OrganizationType.Hospital, " CHU-SO ",
            new OrganizationProfile(ShortName: " CHU-SO ", City: " Lomé "));

        Assert.True(result.IsSuccess);
        var org = result.Value;
        Assert.Equal("CHU Sylvanus Olympio", org.Name); // trim
        Assert.Equal("CHU-SO", org.Code);
        Assert.Equal("CHU-SO", org.ShortName);
        Assert.Equal("Lomé", org.City);
        Assert.Equal(OrganizationStatus.Active, org.Status);
        Assert.Equal("Africa/Lome", org.Timezone); // défaut
        Assert.Null(org.UpdatedOnUtc);
        Assert.NotEqual(Guid.Empty, org.Id);
        var evt = Assert.IsType<OrganizationCreated>(Assert.Single(org.DomainEvents));
        Assert.Equal(org.Id, evt.OrganizationId);
    }

    [Fact]
    public void Create_blank_name_fails()
        => Assert.True(DomainOrganization.Create("   ", OrganizationType.Clinic).IsFailure);

    [Fact]
    public void Create_too_long_name_fails()
        => Assert.True(DomainOrganization.Create(new string('a', DomainOrganization.NameMaxLength + 1), OrganizationType.Unit).IsFailure);

    [Fact]
    public void Create_empty_code_is_normalized_to_null()
        => Assert.Null(DomainOrganization.Create("Clinique Biasa", OrganizationType.Clinic, "   ").Value.Code);

    [Fact]
    public void Update_changes_fields_and_sets_updated()
    {
        var org = DomainOrganization.Create("Ancien", OrganizationType.Clinic).Value;
        var result = org.Update("Nouveau", new OrganizationProfile(Phone: " 90000000 ", Timezone: "Africa/Abidjan"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Nouveau", org.Name);
        Assert.Equal("90000000", org.Phone);
        Assert.Equal("Africa/Abidjan", org.Timezone);
        Assert.NotNull(org.UpdatedOnUtc);
    }

    [Fact]
    public void Update_blank_name_fails()
    {
        var org = DomainOrganization.Create("Valide", OrganizationType.Clinic).Value;
        Assert.True(org.Update("  ", new OrganizationProfile()).IsFailure);
    }

    [Fact]
    public void Deactivate_then_activate_toggles_status()
    {
        var org = DomainOrganization.Create("Service X", OrganizationType.Department).Value;
        org.Deactivate();
        Assert.Equal(OrganizationStatus.Inactive, org.Status);
        org.Activate();
        Assert.Equal(OrganizationStatus.Active, org.Status);
    }
}
