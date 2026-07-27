using Lafie.Eventing.Abstractions;
using Lafie.Eventing.InMemory;
using Xunit;

namespace Lafie.Framework.Tests.Eventing;

public class InMemoryEventBusTests
{
    private sealed record Ping : IntegrationEvent;

    [Fact]
    public async Task Publish_completes()
    {
        IEventBus bus = new InMemoryEventBus();
        await bus.PublishAsync(new Ping());
    }

    [Fact]
    public async Task Publish_null_throws()
    {
        IEventBus bus = new InMemoryEventBus();
        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync(null!));
    }
}
