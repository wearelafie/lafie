using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Liste toutes les organisations.</summary>
public sealed record GetOrganizationsQuery : IRequest<IReadOnlyList<OrganizationDto>>;

public sealed class GetOrganizationsHandler(IOrganizationRepository repository)
    : IRequestHandler<GetOrganizationsQuery, IReadOnlyList<OrganizationDto>>
{
    public async ValueTask<IReadOnlyList<OrganizationDto>> Handle(
        GetOrganizationsQuery query,
        CancellationToken cancellationToken)
        => await repository.GetAllAsync(cancellationToken);
}
