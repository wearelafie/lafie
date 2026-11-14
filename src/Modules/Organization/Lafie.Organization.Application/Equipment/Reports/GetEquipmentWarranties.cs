using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Reports;

/// <summary>Vue globale paginée/filtrée des garanties d'un établissement.</summary>
public sealed record GetEquipmentWarrantiesQuery(Guid FacilityId, EquipmentWarrantyFilter Filter)
    : IRequest<PagedResult<EquipmentWarrantyDto>>;

public sealed class GetEquipmentWarrantiesHandler(IEquipmentReportsRepository reports)
    : IRequestHandler<GetEquipmentWarrantiesQuery, PagedResult<EquipmentWarrantyDto>>
{
    public async ValueTask<PagedResult<EquipmentWarrantyDto>> Handle(GetEquipmentWarrantiesQuery query, CancellationToken cancellationToken)
        => await reports.SearchWarrantiesAsync(query.FacilityId, query.Filter, cancellationToken);
}
