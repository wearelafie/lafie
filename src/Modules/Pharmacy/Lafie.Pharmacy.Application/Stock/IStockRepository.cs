using Lafie.Core.Common;
using Lafie.Pharmacy.Domain;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Critères de recherche des positions de stock d'un établissement.</summary>
public sealed record StockFilter(
    string? Search = null, bool LowOnly = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Métadonnées d'un mouvement de stock à journaliser.</summary>
public sealed record MovementMeta(string? Reason, string? PerformedBy, string? Notes);

/// <summary>Port de persistance du stock pharmaceutique (fiches, lots, mouvements ; schéma `pharmacy`).</summary>
public interface IStockRepository
{
    // Fiches de stock (seuil de réappro).
    Task<StockItem?> FindItemAsync(Guid facilityId, Guid medicationId, CancellationToken cancellationToken = default);
    Task AddItemAsync(StockItem item, CancellationToken cancellationToken = default);
    Task UpdateItemAsync(StockItem item, CancellationToken cancellationToken = default);
    Task<PagedResult<StockItemDto>> SearchStockAsync(Guid facilityId, StockFilter filter, CancellationToken cancellationToken = default);

    // Lots.
    Task AddLotAsync(StockLot lot, CancellationToken cancellationToken = default);
    Task UpdateLotAsync(StockLot lot, CancellationToken cancellationToken = default);
    Task<StockLot?> FindLotAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockLotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockLotDto>> GetLotsAsync(Guid facilityId, Guid medicationId, CancellationToken cancellationToken = default);

    // Mouvements (grand livre).
    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid facilityId, Guid medicationId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sortie de stock FEFO (First Expired First Out) d'un médicament : consomme de façon atomique
    /// les lots actifs par péremption croissante, met à jour les lots et journalise un mouvement
    /// <c>Issue</c> par lot touché. Échoue sans rien modifier si le stock est insuffisant.
    /// </summary>
    Task<Result> IssueFefoAsync(
        Guid facilityId, Guid medicationId, int quantity, StockMovementType type, MovementMeta meta,
        CancellationToken cancellationToken = default);
}
