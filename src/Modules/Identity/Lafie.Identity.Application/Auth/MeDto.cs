using Lafie.Identity.Application.Users;

namespace Lafie.Identity.Application.Auth;

/// <summary>Profil de l'utilisateur connecté (renvoyé par /me et à la connexion).</summary>
public sealed record MeDto(
    Guid Id,
    string Username,
    string Email,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? Title,
    string UserType,
    string? PreferredLanguage,
    string? AvatarUrl,
    Guid? PractitionerId,
    EstablishmentRef? ActiveEstablishment,
    IReadOnlyList<EstablishmentRef> Establishments,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
