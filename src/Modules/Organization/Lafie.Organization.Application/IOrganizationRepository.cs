using Lafie.Core.Common;
using DomainOrganization = Lafie.Organization.Domain.Organization;

namespace Lafie.Organization.Application;

/// <summary>Port de persistance du module Organization (implémenté en Infrastructure/Dapper).</summary>
public interface IOrganizationRepository
{
    Task AddAsync(DomainOrganization organization, CancellationToken cancellationToken = default);

    Task UpdateAsync(DomainOrganization organization, CancellationToken cancellationToken = default);

    Task<DomainOrganization?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrganizationDto>> SearchAsync(OrganizationFilter filter, CancellationToken cancellationToken = default);

    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
