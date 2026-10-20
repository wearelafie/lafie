using Lafie.Organization.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Tests;

public class BuildingTests
{
    private static readonly Guid Facility = Guid.NewGuid();

    [Fact]
    public void Create_valid_succeeds_and_raises_event()
    {
        var result = Building.Create(Facility, "  BAT-A  ", "  Bâtiment A  ", " desc ", " aile est ");

        Assert.True(result.IsSuccess);
        var b = result.Value;
        Assert.Equal("BAT-A", b.Code);
        Assert.Equal("Bâtiment A", b.Name);
        Assert.Equal("desc", b.Description);
        Assert.Equal("aile est", b.Location);
        Assert.Equal(BuildingStatus.Active, b.Status);
        Assert.Equal(Facility, b.FacilityId);
        Assert.Null(b.UpdatedOnUtc);
        var evt = Assert.IsType<BuildingCreated>(Assert.Single(b.DomainEvents));
        Assert.Equal(b.Id, evt.BuildingId);
        Assert.Equal(Facility, evt.FacilityId);
    }

    [Fact]
    public void Create_empty_facility_fails()
        => Assert.True(Building.Create(Guid.Empty, "C", "N").IsFailure);

    [Fact]
    public void Create_blank_name_fails()
        => Assert.True(Building.Create(Facility, "C", "   ").IsFailure);

    [Fact]
    public void Create_blank_code_fails()
        => Assert.True(Building.Create(Facility, "   ", "N").IsFailure);

    [Fact]
    public void Update_changes_fields_and_sets_updated()
    {
        var b = Building.Create(Facility, "C1", "Ancien").Value;
        var r = b.Update("C2", "Nouveau", "d", "l");
        Assert.True(r.IsSuccess);
        Assert.Equal("C2", b.Code);
        Assert.Equal("Nouveau", b.Name);
        Assert.NotNull(b.UpdatedOnUtc);
    }

    [Fact]
    public void SetStatus_updates_status()
    {
        var b = Building.Create(Facility, "C1", "N").Value;
        b.SetStatus(BuildingStatus.Maintenance);
        Assert.Equal(BuildingStatus.Maintenance, b.Status);
        Assert.NotNull(b.UpdatedOnUtc);
    }
}
