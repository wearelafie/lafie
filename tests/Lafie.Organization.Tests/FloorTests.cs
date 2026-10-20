using Lafie.Organization.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Tests;

public class FloorTests
{
    private static readonly Guid Bld = Guid.NewGuid();

    [Fact]
    public void Create_valid_supports_negative_level_and_raises_event()
    {
        var r = Floor.Create(Bld, " SS1 ", " Sous-sol 1 ", level: -1, displayOrder: 2, description: " parking ");
        Assert.True(r.IsSuccess);
        var f = r.Value;
        Assert.Equal("SS1", f.Code);
        Assert.Equal("Sous-sol 1", f.Name);
        Assert.Equal(-1, f.Level);
        Assert.Equal(2, f.DisplayOrder);
        Assert.Equal("parking", f.Description);
        Assert.Equal(FloorStatus.Active, f.Status);
        var evt = Assert.IsType<FloorCreated>(Assert.Single(f.DomainEvents));
        Assert.Equal(f.Id, evt.FloorId);
        Assert.Equal(Bld, evt.BuildingId);
    }

    [Fact]
    public void Create_allows_null_level()
        => Assert.Null(Floor.Create(Bld, "T", "Technique", level: null).Value.Level);

    [Fact]
    public void Create_empty_building_fails() => Assert.True(Floor.Create(Guid.Empty, "C", "N").IsFailure);

    [Fact]
    public void Create_blank_name_fails() => Assert.True(Floor.Create(Bld, "C", "  ").IsFailure);

    [Fact]
    public void Create_blank_code_fails() => Assert.True(Floor.Create(Bld, "  ", "N").IsFailure);

    [Fact]
    public void Update_changes_fields_and_sets_updated()
    {
        var f = Floor.Create(Bld, "C1", "RDC", 0).Value;
        var r = f.Update("C2", "Étage 1", 1, 5, "desc");
        Assert.True(r.IsSuccess);
        Assert.Equal("C2", f.Code);
        Assert.Equal(1, f.Level);
        Assert.Equal(5, f.DisplayOrder);
        Assert.NotNull(f.UpdatedOnUtc);
    }

    [Fact]
    public void SetStatus_updates()
    {
        var f = Floor.Create(Bld, "C1", "N").Value;
        f.SetStatus(FloorStatus.Maintenance);
        Assert.Equal(FloorStatus.Maintenance, f.Status);
    }
}
