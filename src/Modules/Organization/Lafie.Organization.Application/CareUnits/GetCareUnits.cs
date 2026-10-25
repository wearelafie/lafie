using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.CareUnits;

/// <summary>Liste paginée/filtrée des unités de soins d'un service.</summary>
public sealed record GetCareUnitsQuery(Guid ServiceId, CareUnitFilter Filter)
    : IRequest<PagedResult<CareUnitDto>>;

public sealed class GetCareUnitsHandler(ICareUnitRepository careUnits)
    : IRequestHandler<GetCareUnitsQuery, PagedResult<CareUnitDto>>
{
    public async ValueTask<PagedResult<CareUnitDto>> Handle(GetCareUnitsQuery query, CancellationToken cancellationToken)
        => await careUnits.SearchByServiceAsync(query.ServiceId, query.Filter, cancellationToken);
}

/// <summary>Récupère une unité de soins par identifiant.</summary>
public sealed record GetCareUnitByIdQuery(Guid Id) : IRequest<CareUnitDto?>;

public sealed class GetCareUnitByIdHandler(ICareUnitRepository careUnits)
    : IRequestHandler<GetCareUnitByIdQuery, CareUnitDto?>
{
    public async ValueTask<CareUnitDto?> Handle(GetCareUnitByIdQuery query, CancellationToken cancellationToken)
        => await careUnits.GetByIdAsync(query.Id, cancellationToken);
}
