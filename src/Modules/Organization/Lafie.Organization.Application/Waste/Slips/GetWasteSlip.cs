using Lafie.Core.Common;
using Lafie.Organization.Application.Waste.Collections;
using Mediator;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Détail d'un bordereau avec ses collectes rattachées.</summary>
public sealed record WasteSlipDetailDto(WasteSlipDto Slip, IReadOnlyList<WasteCollectionDto> Collections);

public sealed record GetWasteSlipQuery(Guid Id) : IRequest<Result<WasteSlipDetailDto>>;

public sealed class GetWasteSlipHandler(
    IWasteSlipRepository slips,
    IWasteCollectionRepository collections) : IRequestHandler<GetWasteSlipQuery, Result<WasteSlipDetailDto>>
{
    public async ValueTask<Result<WasteSlipDetailDto>> Handle(GetWasteSlipQuery query, CancellationToken cancellationToken)
    {
        var slip = await slips.GetByIdAsync(query.Id, cancellationToken);
        if (slip is null) return Result.Failure<WasteSlipDetailDto>("Bordereau introuvable.");

        var lines = await collections.GetBySlipAsync(query.Id, cancellationToken);
        return Result.Success(new WasteSlipDetailDto(slip, lines));
    }
}
