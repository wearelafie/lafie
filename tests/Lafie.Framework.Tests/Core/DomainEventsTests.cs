using Lafie.Core.Domain;
using Xunit;

namespace Lafie.Framework.Tests.Core;

public class DomainEventsTests
{
    private sealed record ThingCreated(Guid EventId, DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

    private sealed class Thing : AggregateRoot<Guid>
    {
        public Thing() => Id = Guid.NewGuid();
        public void DoSomething() => AddDomainEvent(DomainEvent.Create((id, at) => new ThingCreated(id, at)));
    }

    [Fact]
    public void Raising_records_domain_event()
    {
        var thing = new Thing();
        thing.DoSomething();
        Assert.Single(thing.DomainEvents);
        Assert.IsType<ThingCreated>(thing.DomainEvents.First());
    }

    [Fact]
    public void Clear_empties_domain_events()
    {
        var thing = new Thing();
        thing.DoSomething();
        thing.ClearDomainEvents();
        Assert.Empty(thing.DomainEvents);
    }

    [Fact]
    public void Create_sets_id_and_timestamp()
    {
        var e = DomainEvent.Create((id, at) => new ThingCreated(id, at));
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.True(e.OccurredOnUtc <= DateTimeOffset.UtcNow);
    }
}
