using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Application;

/// <summary>Port de persistance du module Organization (implémenté en Infrastructure/Dapper).</summary>
public interface IOrganizationRepository
{
    Task AddAsync(DomainOrganization organization, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
