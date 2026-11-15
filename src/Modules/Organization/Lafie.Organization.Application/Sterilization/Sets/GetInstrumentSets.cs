using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Liste paginée et filtrée des boîtes d'un établissement.</summary>
public sealed record GetInstrumentSetsQuery(Guid FacilityId, InstrumentSetFilter Filter)
    : IRequest<Result<PagedResult<InstrumentSetDto>>>;

public sealed class GetInstrumentSetsHandler(IInstrumentSetRepository sets)
    : IRequestHandler<GetInstrumentSetsQuery, Result<PagedResult<InstrumentSetDto>>>
{
    public async ValueTask<Result<PagedResult<InstrumentSetDto>>> Handle(GetInstrumentSetsQuery query, CancellationToken cancellationToken)
    {
        var page = await sets.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
