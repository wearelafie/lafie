namespace Lafie.Eventing.Outbox;

/// <summary>Store Outbox (implémentation Dapper à venir — PAS EF Core).</summary>
public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}
