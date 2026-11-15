using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Liste paginée et filtrée des cycles d'un établissement.</summary>
public sealed record GetSterilizationCyclesQuery(Guid FacilityId, SterilizationCycleFilter Filter)
    : IRequest<Result<PagedResult<SterilizationCycleDto>>>;

public sealed class GetSterilizationCyclesHandler(ISterilizationCycleRepository cycles)
    : IRequestHandler<GetSterilizationCyclesQuery, Result<PagedResult<SterilizationCycleDto>>>
{
    public async ValueTask<Result<PagedResult<SterilizationCycleDto>>> Handle(GetSterilizationCyclesQuery query, CancellationToken cancellationToken)
    {
        var page = await cycles.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
