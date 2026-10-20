using Lafie.Core.Common;
using Lafie.Core.Domain;
using Lafie.Organization.Domain.Events;

namespace Lafie.Organization.Domain;

/// <summary>
/// Agrégat Organization : établissement, service, unité de soins ou localisation.
/// Peut être hiérarchique via <see cref="ParentId"/>.
/// </summary>
public sealed class Organization : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;

    public string Name { get; private set; }
    public OrganizationType Type { get; private set; }
    public string? Code { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public string? ShortName { get; private set; }
    public string? Description { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string Timezone { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public Guid? ParentId { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private Organization(
        Guid id,
        string name,
        OrganizationType type,
        string? code,
        OrganizationStatus status,
        OrganizationProfile profile,
        Guid? parentId,
        DateTimeOffset createdOnUtc,
        DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        Name = name;
        Type = type;
        Code = code;
        Status = status;
        ShortName = profile.ShortName;
        Description = profile.Description;
        Phone = profile.Phone;
        Email = profile.Email;
        Timezone = profile.Timezone;
        City = profile.City;
        Country = profile.Country;
        ParentId = parentId;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    /// <summary>Crée une nouvelle organisation (statut Active par défaut).</summary>
    public static Result<Organization> Create(
        string name,
        OrganizationType type,
        string? code = null,
        OrganizationProfile? profile = null,
        Guid? parentId = null)
    {
        var normalized = Normalize(name);
        if (normalized.IsFailure) return Result.Failure<Organization>(normalized.Error!);

        if (!Enum.IsDefined(type))
            return Result.Failure<Organization>("Type d'organisation invalide.");

        var cleanProfile = CleanProfile(profile ?? new OrganizationProfile());

        var org = new Organization(
            Guid.NewGuid(),
            normalized.Value,
            type,
            CleanCode(code),
            OrganizationStatus.Active,
            cleanProfile,
            parentId,
            DateTimeOffset.UtcNow,
            updatedOnUtc: null);

        org.AddDomainEvent(DomainEvent.Create((id, on) =>
            new OrganizationCreated(id, on, org.Id, org.Name, org.Type)));

        return Result.Success(org);
    }

    /// <summary>Réhydrate un agrégat depuis le stockage (pas de règle ni d'événement).</summary>
    public static Organization Rehydrate(
        Guid id,
        string name,
        OrganizationType type,
        string? code,
        OrganizationStatus status,
        OrganizationProfile profile,
        Guid? parentId,
        DateTimeOffset createdOnUtc,
        DateTimeOffset? updatedOnUtc)
        => new(id, name, type, code, status, profile, parentId, createdOnUtc, updatedOnUtc);

    /// <summary>Met à jour le nom et le profil descriptif.</summary>
    public Result Update(string name, OrganizationProfile profile)
    {
        var normalized = Normalize(name);
        if (normalized.IsFailure) return normalized;

        var clean = CleanProfile(profile);
        Name = normalized.Value;
        ShortName = clean.ShortName;
        Description = clean.Description;
        Phone = clean.Phone;
        Email = clean.Email;
        Timezone = clean.Timezone;
        City = clean.City;
        Country = clean.Country;
        Touch();
        return Result.Success();
    }

    public void Activate()
    {
        Status = OrganizationStatus.Active;
        Touch();
    }

    public void Deactivate()
    {
        Status = OrganizationStatus.Inactive;
        Touch();
    }

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static Result<string> Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<string>("Le nom de l'organisation est obligatoire.");
        name = name.Trim();
        return name.Length > NameMaxLength
            ? Result.Failure<string>($"Le nom dépasse {NameMaxLength} caractères.")
            : Result.Success(name);
    }

    private static string? CleanCode(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    private static OrganizationProfile CleanProfile(OrganizationProfile p)
        => new(
            Blank(p.ShortName),
            Blank(p.Description),
            Blank(p.Phone),
            Blank(p.Email),
            string.IsNullOrWhiteSpace(p.Timezone) ? "Africa/Lome" : p.Timezone.Trim(),
            Blank(p.City),
            Blank(p.Country));

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
