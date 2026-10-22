using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Spaces;

public sealed record ArchiveSpaceCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveSpaceHandler(ISpaceRepository spaces) : IRequestHandler<ArchiveSpaceCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveSpaceCommand command, CancellationToken cancellationToken)
    {
        var s = await spaces.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Espace {command.Id} introuvable.");
        s.Archive();
        await spaces.UpdateAsync(s, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreSpaceCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreSpaceHandler(ISpaceRepository spaces) : IRequestHandler<RestoreSpaceCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreSpaceCommand command, CancellationToken cancellationToken)
    {
        var s = await spaces.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Espace {command.Id} introuvable.");
        s.Restore();
        await spaces.UpdateAsync(s, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement un espace (pas de dépendances pour l'instant — lits à venir).</summary>
public sealed record DeleteSpaceCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteSpaceHandler(ISpaceRepository spaces) : IRequestHandler<DeleteSpaceCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteSpaceCommand command, CancellationToken cancellationToken)
    {
        if (await spaces.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Espace {command.Id} introuvable.");
        await spaces.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
