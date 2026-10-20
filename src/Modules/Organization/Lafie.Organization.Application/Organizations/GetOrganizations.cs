using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Liste paginée/filtrée des organisations.</summary>
public sealed record GetOrganizationsQuery(OrganizationFilter Filter)
    : IRequest<PagedResult<OrganizationDto>>;

public sealed class GetOrganizationsHandler(IOrganizationRepository repository)
    : IRequestHandler<GetOrganizationsQuery, PagedResult<OrganizationDto>>
{
    public async ValueTask<PagedResult<OrganizationDto>> Handle(
        GetOrganizationsQuery query,
        CancellationToken cancellationToken)
        => await repository.SearchAsync(query.Filter, cancellationToken);
}
