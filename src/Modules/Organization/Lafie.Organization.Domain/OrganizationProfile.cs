namespace Lafie.Organization.Domain;

/// <summary>Attributs descriptifs/contact d'une organisation (tous optionnels sauf le fuseau).</summary>
public sealed record OrganizationProfile(
    string? ShortName = null,
    string? Description = null,
    string? Phone = null,
    string? Email = null,
    string Timezone = "Africa/Lome",
    string? City = null,
    string? Country = null);
