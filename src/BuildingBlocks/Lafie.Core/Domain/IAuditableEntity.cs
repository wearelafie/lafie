namespace Lafie.Core.Domain;

/// <summary>Audit d'une entité (renseigné en repository/handler — pas d'interceptor EF).</summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedOnUtc { get; }
    string? CreatedBy { get; }
    DateTimeOffset? LastModifiedOnUtc { get; }
    string? LastModifiedBy { get; }
}
