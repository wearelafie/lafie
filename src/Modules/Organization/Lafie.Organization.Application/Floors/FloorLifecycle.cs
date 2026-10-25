using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Spaces;
using Mediator;

namespace Lafie.Organization.Application.Floors;

public sealed record ArchiveFloorCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveFloorHandler(IFloorRepository floors) : IRequestHandler<ArchiveFloorCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveFloorCommand command, CancellationToken cancellationToken)
    {
        var f = await floors.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Étage {command.Id} introuvable.");
        f.Archive();
        await floors.UpdateAsync(f, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreFloorCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreFloorHandler(IFloorRepository floors) : IRequestHandler<RestoreFloorCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreFloorCommand command, CancellationToken cancellationToken)
    {
        var f = await floors.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Étage {command.Id} introuvable.");
        f.Restore();
        await floors.UpdateAsync(f, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement un étage (bloqué s'il contient des espaces).</summary>
public sealed record DeleteFloorCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteFloorHandler(IFloorRepository floors, ISpaceRepository spaces) : IRequestHandler<DeleteFloorCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteFloorCommand command, CancellationToken cancellationToken)
    {
        if (await floors.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Étage {command.Id} introuvable.");

        var children = await spaces.SearchByFloorAsync(command.Id, new SpaceFilter(IncludeArchived: true, PageSize: 1), cancellationToken);
        if (children.Total > 0)
            return Result.Failure("Suppression impossible : l'étage contient des espaces.");

        await floors.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
