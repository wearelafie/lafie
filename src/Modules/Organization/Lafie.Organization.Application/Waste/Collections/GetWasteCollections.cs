using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Waste.Collections;

/// <summary>Liste paginée et filtrée des collectes d'un établissement.</summary>
public sealed record GetWasteCollectionsQuery(Guid FacilityId, WasteCollectionFilter Filter)
    : IRequest<Result<PagedResult<WasteCollectionDto>>>;

public sealed class GetWasteCollectionsHandler(IWasteCollectionRepository collections)
    : IRequestHandler<GetWasteCollectionsQuery, Result<PagedResult<WasteCollectionDto>>>
{
    public async ValueTask<Result<PagedResult<WasteCollectionDto>>> Handle(GetWasteCollectionsQuery query, CancellationToken cancellationToken)
    {
        var page = await collections.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
