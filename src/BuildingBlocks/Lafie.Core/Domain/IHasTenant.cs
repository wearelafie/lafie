namespace Lafie.Core.Domain;

/// <summary>Associe une entité à un tenant (multitenancy Finbuckle).</summary>
public interface IHasTenant
{
    string TenantId { get; }
}
