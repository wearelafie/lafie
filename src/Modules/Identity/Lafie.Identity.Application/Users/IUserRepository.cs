using Lafie.Identity.Domain;

namespace Lafie.Identity.Application.Users;

/// <summary>Établissement d'appartenance d'un utilisateur (tenant).</summary>
public sealed record EstablishmentRef(Guid FacilityId, string Name, bool IsDefault, bool Active);

/// <summary>Port de persistance des comptes utilisateurs.</summary>
public interface IUserRepository
{
    /// <summary>Recherche un compte par e-mail OU nom d'utilisateur (insensible à la casse).</summary>
    Task<User?> FindByLoginAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string email, string username, CancellationToken cancellationToken = default);

    /// <summary>Rôles de l'utilisateur : rôles plateforme (scope vide) + rôles de l'établissement actif.</summary>
    Task<IReadOnlyList<string>> GetRoleCodesAsync(Guid userId, Guid? activeFacilityId = null, CancellationToken cancellationToken = default);
    /// <summary>Permissions effectives : union des rôles plateforme + rôles de l'établissement actif.</summary>
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, Guid? activeFacilityId = null, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, string roleCode, string scope = "", CancellationToken cancellationToken = default);

    // --- Appartenances multi-établissements ---
    Task<IReadOnlyList<EstablishmentRef>> GetEstablishmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid userId, Guid facilityId, CancellationToken cancellationToken = default);
    Task AddEstablishmentAsync(Guid userId, Guid facilityId, string facilityName, bool isDefault, CancellationToken cancellationToken = default);
}
