using Lafie.Core.Common;
using Mediator;

namespace Lafie.Pharmacy.Application.Catalog;

/// <summary>Liste paginée et filtrée du catalogue médicaments d'un établissement.</summary>
public sealed record GetMedicationsQuery(Guid FacilityId, MedicationFilter Filter)
    : IRequest<Result<PagedResult<MedicationDto>>>;

public sealed class GetMedicationsHandler(IMedicationRepository medications)
    : IRequestHandler<GetMedicationsQuery, Result<PagedResult<MedicationDto>>>
{
    public async ValueTask<Result<PagedResult<MedicationDto>>> Handle(GetMedicationsQuery query, CancellationToken cancellationToken)
    {
        var page = await medications.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
