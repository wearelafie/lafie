using Lafie.Core.Common;
using Mediator;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>Détail d'une tâche de bio-nettoyage avec sa checklist.</summary>
public sealed record CleaningTaskDetailDto(CleaningTaskDto Task, IReadOnlyList<CleaningTaskItemDto> Items);

public sealed record GetCleaningTaskQuery(Guid Id) : IRequest<Result<CleaningTaskDetailDto>>;

public sealed class GetCleaningTaskHandler(ICleaningTaskRepository tasks)
    : IRequestHandler<GetCleaningTaskQuery, Result<CleaningTaskDetailDto>>
{
    public async ValueTask<Result<CleaningTaskDetailDto>> Handle(GetCleaningTaskQuery query, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(query.Id, cancellationToken);
        if (task is null) return Result.Failure<CleaningTaskDetailDto>("Tâche introuvable.");

        var items = await tasks.GetItemsAsync(query.Id, cancellationToken);
        return Result.Success(new CleaningTaskDetailDto(task, items));
    }
}
