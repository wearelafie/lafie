using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Cycles;

/// <summary>Détail d'un cycle avec sa charge (boîtes).</summary>
public sealed record SterilizationCycleDetailDto(SterilizationCycleDto Cycle, IReadOnlyList<CycleSetDto> Load);

public sealed record GetSterilizationCycleQuery(Guid Id) : IRequest<Result<SterilizationCycleDetailDto>>;

public sealed class GetSterilizationCycleHandler(ISterilizationCycleRepository cycles)
    : IRequestHandler<GetSterilizationCycleQuery, Result<SterilizationCycleDetailDto>>
{
    public async ValueTask<Result<SterilizationCycleDetailDto>> Handle(GetSterilizationCycleQuery query, CancellationToken cancellationToken)
    {
        var cycle = await cycles.GetByIdAsync(query.Id, cancellationToken);
        if (cycle is null) return Result.Failure<SterilizationCycleDetailDto>("Cycle introuvable.");

        var load = await cycles.GetLoadAsync(query.Id, cancellationToken);
        return Result.Success(new SterilizationCycleDetailDto(cycle, load));
    }
}
