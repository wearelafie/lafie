using Lafie.Core.Common;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>Position de commande (ligne de liste) : en-tête + cumuls calculés.</summary>
public sealed class PurchaseOrderDto
{
    public Guid Id { get; init; }
    public Guid FacilityId { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly? OrderDate { get; init; }
    public DateOnly? ExpectedOn { get; init; }
    public int LineCount { get; init; }
    public int TotalOrdered { get; init; }
    public int TotalReceived { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Ligne d'un bon de commande (avec libellé médicament joint).</summary>
public sealed class PurchaseOrderLineDto
{
    public Guid Id { get; init; }
    public Guid PurchaseOrderId { get; init; }
    public Guid MedicationId { get; init; }
    public string MedicationCode { get; init; } = string.Empty;
    public string MedicationName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int QuantityOrdered { get; init; }
    public int QuantityReceived { get; init; }
    public decimal? UnitPrice { get; init; }
}

/// <summary>Détail complet d'un bon de commande : en-tête + lignes.</summary>
public sealed record PurchaseOrderDetailDto(PurchaseOrderDto Header, IReadOnlyList<PurchaseOrderLineDto> Lines);

/// <summary>Une ligne à créer dans un bon de commande.</summary>
public sealed record PurchaseOrderLineInput(Guid MedicationId, int QuantityOrdered, decimal? UnitPrice);

/// <summary>Critères de recherche des bons de commande d'un établissement.</summary>
public sealed record PurchaseOrderFilter(string? Search = null, string? Status = null, Guid? SupplierId = null, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des bons de commande et de leurs lignes (schéma `pharmacy`).</summary>
public interface IPurchaseOrderRepository
{
    Task AddAsync(Lafie.Pharmacy.Domain.PurchaseOrder order, IReadOnlyList<Lafie.Pharmacy.Domain.PurchaseOrderLine> lines, CancellationToken cancellationToken = default);
    Task UpdateHeaderAsync(Lafie.Pharmacy.Domain.PurchaseOrder order, CancellationToken cancellationToken = default);
    Task ReplaceLinesAsync(Guid orderId, IReadOnlyList<Lafie.Pharmacy.Domain.PurchaseOrderLine> lines, CancellationToken cancellationToken = default);
    Task UpdateLineAsync(Lafie.Pharmacy.Domain.PurchaseOrderLine line, CancellationToken cancellationToken = default);

    Task<Lafie.Pharmacy.Domain.PurchaseOrder?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lafie.Pharmacy.Domain.PurchaseOrderLine?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lafie.Pharmacy.Domain.PurchaseOrderLine>> GetLineAggregatesAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrderLineDto>> GetLinesAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> ReferenceExistsAsync(Guid facilityId, string reference, CancellationToken cancellationToken = default);
    Task<PagedResult<PurchaseOrderDto>> SearchByFacilityAsync(Guid facilityId, PurchaseOrderFilter filter, CancellationToken cancellationToken = default);
}
