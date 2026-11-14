using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Equipment;

/// <summary>Liste paginée/filtrée des équipements d'un établissement.</summary>
public sealed record GetEquipmentQuery(Guid FacilityId, EquipmentFilter Filter)
    : IRequest<PagedResult<EquipmentDto>>;

public sealed class GetEquipmentHandler(IEquipmentRepository equipment)
    : IRequestHandler<GetEquipmentQuery, PagedResult<EquipmentDto>>
{
    public async ValueTask<PagedResult<EquipmentDto>> Handle(GetEquipmentQuery query, CancellationToken cancellationToken)
        => await equipment.SearchAsync(query.FacilityId, query.Filter, cancellationToken);
}

/// <summary>Récupère un équipement par identifiant.</summary>
public sealed record GetEquipmentByIdQuery(Guid Id) : IRequest<EquipmentDto?>;

public sealed class GetEquipmentByIdHandler(IEquipmentRepository equipment)
    : IRequestHandler<GetEquipmentByIdQuery, EquipmentDto?>
{
    public async ValueTask<EquipmentDto?> Handle(GetEquipmentByIdQuery query, CancellationToken cancellationToken)
        => await equipment.GetByIdAsync(query.Id, cancellationToken);
}
