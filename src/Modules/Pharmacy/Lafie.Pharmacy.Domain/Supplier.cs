using Lafie.Core.Common;
using Lafie.Core.Domain;

namespace Lafie.Pharmacy.Domain;

/// <summary>
/// Fournisseur de produits pharmaceutiques référencé par un établissement.
/// Réutilisé par les bons de commande ; son nom alimente le libellé fournisseur des lots reçus.
/// </summary>
public sealed class Supplier : AggregateRoot<Guid>
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 300;
    public const int NotesMaxLength = 1000;

    public Guid FacilityId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public bool Active { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private Supplier(
        Guid id, Guid facilityId, string code, string name, string? contactName, string? email,
        string? phone, string? address, bool active, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
    {
        Id = id;
        FacilityId = facilityId;
        Code = code;
        Name = name;
        ContactName = contactName;
        Email = email;
        Phone = phone;
        Address = address;
        Active = active;
        Notes = notes;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    public static Result<Supplier> Create(
        Guid facilityId, string code, string name, string? contactName, string? email,
        string? phone, string? address, string? notes)
    {
        if (facilityId == Guid.Empty) return Result.Failure<Supplier>("Établissement obligatoire.");
        if (string.IsNullOrWhiteSpace(code)) return Result.Failure<Supplier>("Le code fournisseur est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<Supplier>("Le nom du fournisseur est obligatoire.");

        return Result.Success(new Supplier(
            Guid.NewGuid(), facilityId, code.Trim()[..Math.Min(code.Trim().Length, CodeMaxLength)],
            Clamp(name, NameMaxLength)!, Clamp(contactName, NameMaxLength), Clamp(email, TextMaxLength),
            Clamp(phone, TextMaxLength), Clamp(address, TextMaxLength), active: true, Clamp(notes, NotesMaxLength),
            DateTimeOffset.UtcNow, null));
    }

    public Result Edit(string name, string? contactName, string? email, string? phone, string? address, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure("Le nom du fournisseur est obligatoire.");
        Name = Clamp(name, NameMaxLength)!;
        ContactName = Clamp(contactName, NameMaxLength);
        Email = Clamp(email, TextMaxLength);
        Phone = Clamp(phone, TextMaxLength);
        Address = Clamp(address, TextMaxLength);
        Notes = Clamp(notes, NotesMaxLength);
        Touch();
        return Result.Success();
    }

    public void SetActive(bool active) { Active = active; Touch(); }

    public static Supplier Rehydrate(
        Guid id, Guid facilityId, string code, string name, string? contactName, string? email,
        string? phone, string? address, bool active, string? notes,
        DateTimeOffset createdOnUtc, DateTimeOffset? updatedOnUtc)
        => new(id, facilityId, code, name, contactName, email, phone, address, active, notes, createdOnUtc, updatedOnUtc);

    private void Touch() => UpdatedOnUtc = DateTimeOffset.UtcNow;

    private static string? Clamp(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}
