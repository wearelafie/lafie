using Lafie.Eventing.Abstractions;
using Lafie.Eventing.Serialization;
using Xunit;

namespace Lafie.Framework.Tests.Eventing;

public class JsonEventSerializerTests
{
    private sealed record PatientRegistered(string Mrn) : IntegrationEvent;

    [Fact]
    public void Serialize_then_deserialize_roundtrips()
    {
        var serializer = new JsonEventSerializer();
        var evt = new PatientRegistered("MRN-123");

        var payload = serializer.Serialize(evt);
        var back = serializer.Deserialize(payload, typeof(PatientRegistered));

        var typed = Assert.IsType<PatientRegistered>(back);
        Assert.Equal("MRN-123", typed.Mrn);
        Assert.Equal(evt.EventId, typed.EventId);
    }
}
