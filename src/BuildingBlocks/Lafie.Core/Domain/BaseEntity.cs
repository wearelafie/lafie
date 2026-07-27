namespace Lafie.Core.Domain;

/// <summary>Base d'entité : identité + collecte des événements de domaine.</summary>
public abstract class BaseEntity<TId> : IEntity<TId>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
