using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Liste paginée et filtrée des tâches de bio-nettoyage d'un établissement.</summary>
public sealed record GetCleaningTasksQuery(Guid FacilityId, CleaningTaskFilter Filter)
    : IRequest<Result<PagedResult<CleaningTaskDto>>>;

public sealed class GetCleaningTasksHandler(ICleaningTaskRepository tasks)
    : IRequestHandler<GetCleaningTasksQuery, Result<PagedResult<CleaningTaskDto>>>
{
    public async ValueTask<Result<PagedResult<CleaningTaskDto>>> Handle(GetCleaningTasksQuery query, CancellationToken cancellationToken)
    {
        var page = await tasks.SearchByFacilityAsync(query.FacilityId, query.Filter, cancellationToken);
        return Result.Success(page);
    }
}
