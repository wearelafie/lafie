namespace Lafie.Core.Domain;

/// <summary>Expose les événements de domaine levés par une entité.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
