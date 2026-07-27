namespace Lafie.Core.Domain;

/// <summary>Racine d'agrégat : seule porte d'entrée transactionnelle d'un agrégat.</summary>
public abstract class AggregateRoot<TId> : BaseEntity<TId>
{
}
