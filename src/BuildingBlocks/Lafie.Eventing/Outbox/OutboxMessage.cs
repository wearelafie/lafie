namespace Lafie.Eventing.Outbox;

/// <summary>Message stocké transactionnellement (pattern Outbox) puis publié de façon fiable.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
