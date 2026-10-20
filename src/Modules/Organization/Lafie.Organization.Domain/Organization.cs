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
    public bool Active { get; private set; }
    public Guid? ParentId { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    private Organization(
        Guid id,
        string name,
        OrganizationType type,
        string? code,
        bool active,
        Guid? parentId,
        DateTimeOffset createdOnUtc)
    {
        Id = id;
        Name = name;
        Type = type;
        Code = code;
        Active = active;
        ParentId = parentId;
        CreatedOnUtc = createdOnUtc;
    }

    /// <summary>Crée une nouvelle organisation (règles métier + événement).</summary>
    public static Result<Organization> Create(
        string name,
        OrganizationType type,
        string? code = null,
        Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Organization>("Le nom de l'organisation est obligatoire.");

        name = name.Trim();
        if (name.Length > NameMaxLength)
            return Result.Failure<Organization>($"Le nom dépasse {NameMaxLength} caractères.");

        if (!Enum.IsDefined(type))
            return Result.Failure<Organization>("Type d'organisation invalide.");

        var org = new Organization(
            Guid.NewGuid(),
            name,
            type,
            string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            active: true,
            parentId,
            DateTimeOffset.UtcNow);

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
        bool active,
        Guid? parentId,
        DateTimeOffset createdOnUtc)
        => new(id, name, type, code, active, parentId, createdOnUtc);

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Le nom de l'organisation est obligatoire.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            return Result.Failure($"Le nom dépasse {NameMaxLength} caractères.");
        Name = name;
        return Result.Success();
    }

    public void Activate() => Active = true;
    public void Deactivate() => Active = false;
}
