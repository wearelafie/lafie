using Lafie.Core.Common;
using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Critères de recherche des tâches de bio-nettoyage d'un établissement.</summary>
public sealed record CleaningTaskFilter(
    string? Search = null, string? Status = null, string? Protocol = null, Guid? SpaceId = null,
    bool ActiveOnly = false, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Offset => (SafePage - 1) * SafePageSize;
}

/// <summary>Port de persistance des tâches de bio-nettoyage (hygiène hospitalière).</summary>
public interface ICleaningTaskRepository
{
    Task AddAsync(CleaningTask task, IReadOnlyList<CleaningTaskItem> items, CancellationToken cancellationToken = default);
    Task UpdateAsync(CleaningTask task, CancellationToken cancellationToken = default);
    Task<CleaningTask?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CleaningTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<CleaningTaskDto>> SearchByFacilityAsync(Guid facilityId, CleaningTaskFilter filter, CancellationToken cancellationToken = default);
    /// <summary>Compte les tâches de nettoyage ACTIVES (Pending/InProgress) ciblant un espace, hors tâche exclue.</summary>
    Task<int> CountActiveForSpaceAsync(Guid spaceId, Guid? excludeTaskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CleaningTaskItemDto>> GetItemsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<CleaningTaskItem?> FindItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task AddItemAsync(CleaningTaskItem item, CancellationToken cancellationToken = default);
    Task UpdateItemAsync(CleaningTaskItem item, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    /// <summary>Vrai si la tâche n'a plus aucune étape non cochée (checklist complète), hors item ignoré.</summary>
    Task<bool> AllItemsDoneAsync(Guid taskId, Guid? ignoreItemId, CancellationToken cancellationToken = default);
    Task<int> NextSortOrderAsync(Guid taskId, CancellationToken cancellationToken = default);
}
