using Lafie.Core.Common;
using Lafie.Organization.Domain;
using Lafie.Organization.Domain.Events;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Tests;

public class OrganizationTests
{
    private static Result<DomainOrganization> Valid(
        string code = "CHU-SO", string name = "CHU Sylvanus Olympio",
        FacilityAddress? address = null, FacilityLegal? legal = null)
        => DomainOrganization.Create(code, name, "HOSPITAL", "PUBLIC", "TG",
            address: address, legal: legal);

    [Fact]
    public void Create_valid_defaults_and_event()
    {
        var r = Valid();
        Assert.True(r.IsSuccess);
        var o = r.Value;
        Assert.Equal("CHU-SO", o.Code);
        Assert.Equal("CHU Sylvanus Olympio", o.OfficialName);
        Assert.Equal("HOSPITAL", o.FacilityTypeCode);
        Assert.Equal("PUBLIC", o.OwnershipTypeCode);
        Assert.Equal("TG", o.CountryCode);
        Assert.Equal("Africa/Lome", o.TimeZoneId);
        Assert.Equal("XOF", o.DefaultCurrencyCode);
        Assert.Equal(OperationalStatus.Active, o.OperationalStatus);
        Assert.Equal(ConfigurationStatus.Draft, o.ConfigurationStatus);
        var evt = Assert.IsType<OrganizationCreated>(Assert.Single(o.DomainEvents));
        Assert.Equal(o.Id, evt.OrganizationId);
        Assert.Equal("HOSPITAL", evt.FacilityTypeCode);
    }

    [Theory]
    [InlineData("", "N", "HOSPITAL", "PUBLIC", "TG")]       // code vide
    [InlineData("C", "", "HOSPITAL", "PUBLIC", "TG")]       // nom vide
    [InlineData("C", "N", "", "PUBLIC", "TG")]              // type vide
    [InlineData("C", "N", "HOSPITAL", "", "TG")]            // ownership vide
    [InlineData("C", "N", "HOSPITAL", "PUBLIC", "TGO")]     // code pays ≠ 2
    public void Create_missing_required_fails(string code, string name, string type, string own, string country)
        => Assert.True(DomainOrganization.Create(code, name, type, own, country).IsFailure);

    [Fact]
    public void Create_latitude_without_longitude_fails()
        => Assert.True(Valid(address: new FacilityAddress(Latitude: 6.13m)).IsFailure);

    [Fact]
    public void Create_latitude_out_of_range_fails()
        => Assert.True(Valid(address: new FacilityAddress(Latitude: 95m, Longitude: 1m)).IsFailure);

    [Fact]
    public void Create_both_coordinates_ok()
    {
        var o = Valid(address: new FacilityAddress(Latitude: 6.13m, Longitude: 1.22m)).Value;
        Assert.Equal(6.13m, o.Latitude);
        Assert.Equal(1.22m, o.Longitude);
    }

    [Fact]
    public void Create_license_expiry_before_issue_fails()
        => Assert.True(Valid(legal: new FacilityLegal(
            LicenseIssuedOn: new DateOnly(2025, 1, 1), LicenseExpiresOn: new DateOnly(2024, 1, 1))).IsFailure);

    [Fact]
    public void Update_changes_fields_and_sets_updated()
    {
        var o = Valid().Value;
        var r = o.Update("CHU-SO", "Nouveau nom", "HOSPITAL", "PRIVATE_NON_PROFIT", "TG",
            new FacilityIdentity(DisplayName: "CHU"), new FacilityAddress(Locality: "Lomé"),
            new FacilityLegal(), new FacilityOperations(PublicFacing: true), new FacilityPrimaryContact(Phone: "90000000"));
        Assert.True(r.IsSuccess);
        Assert.Equal("Nouveau nom", o.OfficialName);
        Assert.Equal("PRIVATE_NON_PROFIT", o.OwnershipTypeCode);
        Assert.Equal("CHU", o.DisplayName);
        Assert.True(o.PublicFacing);
        Assert.Equal("90000000", o.Phone);
        Assert.NotNull(o.UpdatedOnUtc);
    }

    [Fact]
    public void ChangeOperationalStatus_updates()
    {
        var o = Valid().Value;
        o.ChangeOperationalStatus(OperationalStatus.TemporarilyClosed);
        Assert.Equal(OperationalStatus.TemporarilyClosed, o.OperationalStatus);
        Assert.NotNull(o.UpdatedOnUtc);
    }
}
