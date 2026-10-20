using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Récupère une organisation par son identifiant.</summary>
public sealed record GetOrganizationByIdQuery(Guid Id) : IRequest<OrganizationDto?>;

public sealed class GetOrganizationByIdHandler(IOrganizationRepository repository)
    : IRequestHandler<GetOrganizationByIdQuery, OrganizationDto?>
{
    public async ValueTask<OrganizationDto?> Handle(
        GetOrganizationByIdQuery query,
        CancellationToken cancellationToken)
        => await repository.GetByIdAsync(query.Id, cancellationToken);
}
