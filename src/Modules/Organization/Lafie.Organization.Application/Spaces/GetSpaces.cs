using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Spaces;

/// <summary>Liste paginée/filtrée des espaces d'un étage.</summary>
public sealed record GetSpacesQuery(Guid FloorId, SpaceFilter Filter) : IRequest<PagedResult<SpaceDto>>;

public sealed class GetSpacesHandler(ISpaceRepository spaces) : IRequestHandler<GetSpacesQuery, PagedResult<SpaceDto>>
{
    public async ValueTask<PagedResult<SpaceDto>> Handle(GetSpacesQuery query, CancellationToken cancellationToken)
        => await spaces.SearchByFloorAsync(query.FloorId, query.Filter, cancellationToken);
}

/// <summary>Récupère un espace par identifiant.</summary>
public sealed record GetSpaceByIdQuery(Guid Id) : IRequest<SpaceDto?>;

public sealed class GetSpaceByIdHandler(ISpaceRepository spaces) : IRequestHandler<GetSpaceByIdQuery, SpaceDto?>
{
    public async ValueTask<SpaceDto?> Handle(GetSpaceByIdQuery query, CancellationToken cancellationToken)
        => await spaces.GetByIdAsync(query.Id, cancellationToken);
}
