using Lafie.Core.Common;
using Mediator;

namespace Lafie.Pharmacy.Application.Stock;

/// <summary>Liste paginée des positions de stock d'un établissement (médicaments avec stock ou seuil).</summary>
public sealed record GetStockItemsQuery(Guid FacilityId, StockFilter Filter)
    : IRequest<Result<PagedResult<StockItemDto>>>;

public sealed class GetStockItemsHandler(IStockRepository stock)
    : IRequestHandler<GetStockItemsQuery, Result<PagedResult<StockItemDto>>>
{
    public async ValueTask<Result<PagedResult<StockItemDto>>> Handle(GetStockItemsQuery query, CancellationToken cancellationToken)
    {
        var page = await stock.SearchStockAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
