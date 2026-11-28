using Lafie.Core.Common;
using Mediator;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>Liste paginée des bons de commande d'un établissement.</summary>
public sealed record GetPurchaseOrdersQuery(Guid FacilityId, PurchaseOrderFilter Filter)
    : IRequest<Result<PagedResult<PurchaseOrderDto>>>;

public sealed class GetPurchaseOrdersHandler(IPurchaseOrderRepository orders)
    : IRequestHandler<GetPurchaseOrdersQuery, Result<PagedResult<PurchaseOrderDto>>>
{
    public async ValueTask<Result<PagedResult<PurchaseOrderDto>>> Handle(GetPurchaseOrdersQuery query, CancellationToken cancellationToken)
        => Result.Success(await orders.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken));
}

/// <summary>Détail d'un bon de commande : en-tête + lignes.</summary>
public sealed record GetPurchaseOrderQuery(Guid Id) : IRequest<Result<PurchaseOrderDetailDto>>;

public sealed class GetPurchaseOrderHandler(IPurchaseOrderRepository orders)
    : IRequestHandler<GetPurchaseOrderQuery, Result<PurchaseOrderDetailDto>>
{
    public async ValueTask<Result<PurchaseOrderDetailDto>> Handle(GetPurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        var header = await orders.GetByIdAsync(query.Id, cancellationToken);
        if (header is null) return Result.Failure<PurchaseOrderDetailDto>("Bon de commande introuvable.");

        var lines = await orders.GetLinesAsync(query.Id, cancellationToken);
        return Result.Success(new PurchaseOrderDetailDto(header, lines));
    }
}
