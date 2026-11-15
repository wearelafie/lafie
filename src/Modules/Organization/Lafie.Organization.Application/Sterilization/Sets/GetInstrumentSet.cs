using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Sterilization.Sets;

/// <summary>Détail d'une boîte avec son historique d'affectations (traçabilité).</summary>
public sealed record InstrumentSetDetailDto(InstrumentSetDto Set, IReadOnlyList<InstrumentSetUsageDto> Usages);

public sealed record GetInstrumentSetQuery(Guid Id) : IRequest<Result<InstrumentSetDetailDto>>;

public sealed class GetInstrumentSetHandler(IInstrumentSetRepository sets)
    : IRequestHandler<GetInstrumentSetQuery, Result<InstrumentSetDetailDto>>
{
    public async ValueTask<Result<InstrumentSetDetailDto>> Handle(GetInstrumentSetQuery query, CancellationToken cancellationToken)
    {
        var set = await sets.GetByIdAsync(query.Id, cancellationToken);
        if (set is null) return Result.Failure<InstrumentSetDetailDto>("Boîte introuvable.");

        var usages = await sets.GetUsagesAsync(query.Id, cancellationToken);
        return Result.Success(new InstrumentSetDetailDto(set, usages));
    }
}
