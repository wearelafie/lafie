using Lafie.Organization.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Tests;

public class SpaceTests
{
    private static readonly Guid F = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid Fl = Guid.NewGuid();

    [Fact]
    public void Create_valid_succeeds_and_raises_event()
    {
        var r = Space.Create(F, B, Fl, " CH-101 ", " Chambre 101 ", SpaceType.PatientRoom, " std ", area: 18.5m, maxCapacity: 2, accessible: true);
        Assert.True(r.IsSuccess);
        var s = r.Value;
        Assert.Equal("CH-101", s.Code);
        Assert.Equal(SpaceType.PatientRoom, s.Type);
        Assert.Equal(18.5m, s.Area);
        Assert.Equal(2, s.MaxCapacity);
        Assert.True(s.Accessible);
        Assert.Equal(SpaceStatus.Active, s.Status);
        var evt = Assert.IsType<SpaceCreated>(Assert.Single(s.DomainEvents));
        Assert.Equal(s.Id, evt.SpaceId);
        Assert.Equal(Fl, evt.FloorId);
    }

    [Fact]
    public void Create_without_lineage_fails()
        => Assert.True(Space.Create(Guid.Empty, B, Fl, "C", "N", SpaceType.Office).IsFailure);

    [Fact]
    public void Create_blank_name_fails()
        => Assert.True(Space.Create(F, B, Fl, "C", "  ", SpaceType.Office).IsFailure);

    [Fact]
    public void Create_negative_area_fails()
        => Assert.True(Space.Create(F, B, Fl, "C", "N", SpaceType.Office, area: -1m).IsFailure);

    [Fact]
    public void Create_negative_capacity_fails()
        => Assert.True(Space.Create(F, B, Fl, "C", "N", SpaceType.Office, maxCapacity: -2).IsFailure);

    [Fact]
    public void Create_allows_no_beds_space()
    {
        var s = Space.Create(F, B, Fl, "ATT-1", "Salle d'attente", SpaceType.WaitingRoom).Value;
        Assert.Null(s.MaxCapacity);
        Assert.Null(s.Area);
    }

    [Fact]
    public void Update_changes_fields_and_sets_updated()
    {
        var s = Space.Create(F, B, Fl, "C1", "Avant", SpaceType.Office).Value;
        var r = s.Update("C2", "Après", SpaceType.ConsultationRoom, "d", 12m, 1, true);
        Assert.True(r.IsSuccess);
        Assert.Equal(SpaceType.ConsultationRoom, s.Type);
        Assert.Equal(12m, s.Area);
        Assert.NotNull(s.UpdatedOnUtc);
    }

    [Fact]
    public void SetStatus_updates()
    {
        var s = Space.Create(F, B, Fl, "C1", "N", SpaceType.Office).Value;
        s.SetStatus(SpaceStatus.Maintenance);
        Assert.Equal(SpaceStatus.Maintenance, s.Status);
    }
}
