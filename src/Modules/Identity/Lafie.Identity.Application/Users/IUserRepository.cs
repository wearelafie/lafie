using Lafie.Identity.Domain;

namespace Lafie.Identity.Application.Users;

/// <summary>Port de persistance des comptes utilisateurs.</summary>
public interface IUserRepository
{
    /// <summary>Recherche un compte par e-mail OU nom d'utilisateur (insensible à la casse).</summary>
    Task<User?> FindByLoginAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string email, string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRoleCodesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, string roleCode, CancellationToken cancellationToken = default);
}
