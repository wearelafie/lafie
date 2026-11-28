using Lafie.Core.Common;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>Projection lecture d'un fournisseur.</summary>
public sealed class SupplierDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public bool Active { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

/// <summary>Critères de recherche des fournisseurs d'un établissement.</summary>
public sealed record SupplierFilter(string? Search = null, bool? Active = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des fournisseurs (schéma `pharmacy`).</summary>
public interface ISupplierRepository
{
    Task AddAsync(Lafie.Pharmacy.Domain.Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lafie.Pharmacy.Domain.Supplier supplier, CancellationToken cancellationToken = default);
    Task<Lafie.Pharmacy.Domain.Supplier?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid facilityId, string code, CancellationToken cancellationToken = default);
    Task<PagedResult<SupplierDto>> SearchByFacilityAsync(Guid facilityId, SupplierFilter filter, CancellationToken cancellationToken = default);
}
