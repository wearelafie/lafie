using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Buildings;

/// <summary>Liste paginée/filtrée des bâtiments d'un établissement.</summary>
public sealed record GetBuildingsQuery(Guid FacilityId, BuildingFilter Filter)
    : IRequest<PagedResult<BuildingDto>>;

public sealed class GetBuildingsHandler(IBuildingRepository buildings)
    : IRequestHandler<GetBuildingsQuery, PagedResult<BuildingDto>>
{
    public async ValueTask<PagedResult<BuildingDto>> Handle(GetBuildingsQuery query, CancellationToken cancellationToken)
        => await buildings.SearchAsync(query.FacilityId, query.Filter, cancellationToken);
}

/// <summary>Récupère un bâtiment par identifiant.</summary>
public sealed record GetBuildingByIdQuery(Guid Id) : IRequest<BuildingDto?>;

public sealed class GetBuildingByIdHandler(IBuildingRepository buildings)
    : IRequestHandler<GetBuildingByIdQuery, BuildingDto?>
{
    public async ValueTask<BuildingDto?> Handle(GetBuildingByIdQuery query, CancellationToken cancellationToken)
        => await buildings.GetByIdAsync(query.Id, cancellationToken);
}
