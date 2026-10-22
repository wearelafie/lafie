using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Floors;
using Mediator;

namespace Lafie.Organization.Application.Buildings;

public sealed record ArchiveBuildingCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveBuildingHandler(IBuildingRepository buildings) : IRequestHandler<ArchiveBuildingCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveBuildingCommand command, CancellationToken cancellationToken)
    {
        var b = await buildings.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Bâtiment {command.Id} introuvable.");
        b.Archive();
        await buildings.UpdateAsync(b, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreBuildingCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreBuildingHandler(IBuildingRepository buildings) : IRequestHandler<RestoreBuildingCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreBuildingCommand command, CancellationToken cancellationToken)
    {
        var b = await buildings.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Bâtiment {command.Id} introuvable.");
        b.Restore();
        await buildings.UpdateAsync(b, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement un bâtiment (bloqué s'il contient des étages).</summary>
public sealed record DeleteBuildingCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteBuildingHandler(IBuildingRepository buildings, IFloorRepository floors) : IRequestHandler<DeleteBuildingCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteBuildingCommand command, CancellationToken cancellationToken)
    {
        if (await buildings.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Bâtiment {command.Id} introuvable.");

        var children = await floors.SearchAsync(command.Id, new FloorFilter(IncludeArchived: true, PageSize: 1), cancellationToken);
        if (children.Total > 0)
            return Result.Failure("Suppression impossible : le bâtiment contient des étages.");

        await buildings.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
