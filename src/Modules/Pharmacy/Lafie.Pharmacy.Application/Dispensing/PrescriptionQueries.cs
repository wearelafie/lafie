using Lafie.Core.Common;
using Mediator;

namespace Lafie.Pharmacy.Application.Dispensing;

/// <summary>Liste paginée des prescriptions d'un établissement.</summary>
public sealed record GetPrescriptionsQuery(Guid FacilityId, PrescriptionFilter Filter)
    : IRequest<Result<PagedResult<PrescriptionDto>>>;

public sealed class GetPrescriptionsHandler(IPrescriptionRepository prescriptions)
    : IRequestHandler<GetPrescriptionsQuery, Result<PagedResult<PrescriptionDto>>>
{
    public async ValueTask<Result<PagedResult<PrescriptionDto>>> Handle(GetPrescriptionsQuery query, CancellationToken cancellationToken)
        => Result.Success(await prescriptions.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken));
}

/// <summary>Détail d'une prescription : en-tête + lignes.</summary>
public sealed record GetPrescriptionQuery(Guid Id) : IRequest<Result<PrescriptionDetailDto>>;

public sealed class GetPrescriptionHandler(IPrescriptionRepository prescriptions)
    : IRequestHandler<GetPrescriptionQuery, Result<PrescriptionDetailDto>>
{
    public async ValueTask<Result<PrescriptionDetailDto>> Handle(GetPrescriptionQuery query, CancellationToken cancellationToken)
    {
        var header = await prescriptions.GetByIdAsync(query.Id, cancellationToken);
        if (header is null) return Result.Failure<PrescriptionDetailDto>("Prescription introuvable.");

        var lines = await prescriptions.GetLinesAsync(query.Id, cancellationToken);
        return Result.Success(new PrescriptionDetailDto(header, lines));
    }
}
