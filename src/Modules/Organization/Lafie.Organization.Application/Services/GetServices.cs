using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Services;

/// <summary>Liste paginée/filtrée des services d'un établissement.</summary>
public sealed record GetServicesQuery(Guid FacilityId, ServiceFilter Filter)
    : IRequest<PagedResult<ServiceDto>>;

public sealed class GetServicesHandler(IServiceRepository services)
    : IRequestHandler<GetServicesQuery, PagedResult<ServiceDto>>
{
    public async ValueTask<PagedResult<ServiceDto>> Handle(GetServicesQuery query, CancellationToken cancellationToken)
        => await services.SearchAsync(query.FacilityId, query.Filter, cancellationToken);
}

/// <summary>Récupère un service par identifiant.</summary>
public sealed record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto?>;

public sealed class GetServiceByIdHandler(IServiceRepository services)
    : IRequestHandler<GetServiceByIdQuery, ServiceDto?>
{
    public async ValueTask<ServiceDto?> Handle(GetServiceByIdQuery query, CancellationToken cancellationToken)
        => await services.GetByIdAsync(query.Id, cancellationToken);
}
