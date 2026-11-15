using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Liste paginée et filtrée des bordereaux d'un établissement.</summary>
public sealed record GetWasteSlipsQuery(Guid FacilityId, WasteSlipFilter Filter)
    : IRequest<Result<PagedResult<WasteSlipDto>>>;

public sealed class GetWasteSlipsHandler(IWasteSlipRepository slips)
    : IRequestHandler<GetWasteSlipsQuery, Result<PagedResult<WasteSlipDto>>>
{
    public async ValueTask<Result<PagedResult<WasteSlipDto>>> Handle(GetWasteSlipsQuery query, CancellationToken cancellationToken)
    {
        var page = await slips.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
