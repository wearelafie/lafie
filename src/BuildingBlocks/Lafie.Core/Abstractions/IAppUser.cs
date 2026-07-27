namespace Lafie.Core.Abstractions;

/// <summary>Représentation minimale d'un utilisateur applicatif (indépendante du transport).</summary>
public interface IAppUser
{
    Guid Id { get; }
    string? Email { get; }
    string? Name { get; }
}
