using Lafie.Core.Common;
using Mediator;

namespace Lafie.Pharmacy.Application.Purchasing;

/// <summary>Liste paginée des fournisseurs d'un établissement.</summary>
public sealed record GetSuppliersQuery(Guid FacilityId, SupplierFilter Filter) : IRequest<Result<PagedResult<SupplierDto>>>;

public sealed class GetSuppliersHandler(ISupplierRepository suppliers)
    : IRequestHandler<GetSuppliersQuery, Result<PagedResult<SupplierDto>>>
{
    public async ValueTask<Result<PagedResult<SupplierDto>>> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
        => Result.Success(await suppliers.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken));
}

/// <summary>Détail d'un fournisseur.</summary>
public sealed record GetSupplierQuery(Guid Id) : IRequest<Result<SupplierDto>>;

public sealed class GetSupplierHandler(ISupplierRepository suppliers)
    : IRequestHandler<GetSupplierQuery, Result<SupplierDto>>
{
    public async ValueTask<Result<SupplierDto>> Handle(GetSupplierQuery query, CancellationToken cancellationToken)
    {
        var dto = await suppliers.GetByIdAsync(query.Id, cancellationToken);
        return dto is null ? Result.Failure<SupplierDto>("Fournisseur introuvable.") : Result.Success(dto);
    }
}
