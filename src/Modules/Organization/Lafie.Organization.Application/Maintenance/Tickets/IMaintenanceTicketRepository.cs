using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Maintenance.Tickets;

/// <summary>Critères de recherche des tickets de maintenance d'un établissement.</summary>
public sealed record MaintenanceTicketFilter(
    string? Search = null, string? Status = null, string? Criticality = null, string? TargetType = null,
    Guid? TargetId = null, bool OpenOnly = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des tickets de maintenance (GMAO).</summary>
public interface IMaintenanceTicketRepository
{
    Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MaintenanceTicketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<MaintenanceTicketDto>> SearchByFacilityAsync(Guid facilityId, MaintenanceTicketFilter filter, CancellationToken cancellationToken = default);
    /// <summary>Compte les tickets bloquants ACTIFS (non résolus/clôturés) ciblant un espace, hors ticket exclu.</summary>
    Task<int> CountActiveBlockingForSpaceAsync(Guid spaceId, Guid? excludeTicketId, CancellationToken cancellationToken = default);

    Task AddPartAsync(MaintenanceTicketPart part, CancellationToken cancellationToken = default);
    Task DeletePartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicketPartDto>> GetPartsAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
