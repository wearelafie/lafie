namespace Lafie.Core.Domain;

/// <summary>Suppression logique (filtre SQL deleted_at is null).</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedOnUtc { get; }
    string? DeletedBy { get; }
}
