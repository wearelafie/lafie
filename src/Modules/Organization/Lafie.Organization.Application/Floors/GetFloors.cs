using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Floors;

/// <summary>Liste paginée/filtrée des étages d'un bâtiment.</summary>
public sealed record GetFloorsQuery(Guid BuildingId, FloorFilter Filter) : IRequest<PagedResult<FloorDto>>;

public sealed class GetFloorsHandler(IFloorRepository floors) : IRequestHandler<GetFloorsQuery, PagedResult<FloorDto>>
{
    public async ValueTask<PagedResult<FloorDto>> Handle(GetFloorsQuery query, CancellationToken cancellationToken)
        => await floors.SearchAsync(query.BuildingId, query.Filter, cancellationToken);
}

/// <summary>Récupère un étage par identifiant.</summary>
public sealed record GetFloorByIdQuery(Guid Id) : IRequest<FloorDto?>;

public sealed class GetFloorByIdHandler(IFloorRepository floors) : IRequestHandler<GetFloorByIdQuery, FloorDto?>
{
    public async ValueTask<FloorDto?> Handle(GetFloorByIdQuery query, CancellationToken cancellationToken)
        => await floors.GetByIdAsync(query.Id, cancellationToken);
}
