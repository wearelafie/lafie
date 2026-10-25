using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Services;

/// <summary>Critères de recherche/pagination des services (au sein d'un établissement).</summary>
public sealed record ServiceFilter(string? Search = null, string? Category = null, string? Status = null, bool IncludeArchived = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des services.</summary>
public interface IServiceRepository
{
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    Task UpdateAsync(Service service, CancellationToken cancellationToken = default);
    Task<Service?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ServiceDto>> SearchAsync(Guid facilityId, ServiceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Vrai si un code existe déjà dans l'établissement (hors service <paramref name="excludeId"/>).</summary>
    Task<bool> CodeExistsAsync(Guid facilityId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Vrai si le service a au moins un sous-service (archivé ou non).</summary>
    Task<bool> HasChildrenAsync(Guid serviceId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
