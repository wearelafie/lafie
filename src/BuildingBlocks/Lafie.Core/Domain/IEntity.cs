namespace Lafie.Core.Domain;

/// <summary>Entité avec identifiant fortement typé.</summary>
public interface IEntity<out TId>
{
    TId Id { get; }
}
