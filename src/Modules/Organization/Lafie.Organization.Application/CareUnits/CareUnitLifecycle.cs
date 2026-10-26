using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.CareUnits;

public sealed record ArchiveCareUnitCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveCareUnitHandler(ICareUnitRepository careUnits) : IRequestHandler<ArchiveCareUnitCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveCareUnitCommand command, CancellationToken cancellationToken)
    {
        var u = await careUnits.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Unité de soins {command.Id} introuvable.");
        u.Archive();
        await careUnits.UpdateAsync(u, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreCareUnitCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreCareUnitHandler(ICareUnitRepository careUnits) : IRequestHandler<RestoreCareUnitCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreCareUnitCommand command, CancellationToken cancellationToken)
    {
        var u = await careUnits.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Unité de soins {command.Id} introuvable.");
        u.Restore();
        await careUnits.UpdateAsync(u, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement une unité de soins (ses associations aux espaces sont retirées en cascade).</summary>
public sealed record DeleteCareUnitCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCareUnitHandler(ICareUnitRepository careUnits) : IRequestHandler<DeleteCareUnitCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteCareUnitCommand command, CancellationToken cancellationToken)
    {
        if (await careUnits.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Unité de soins {command.Id} introuvable.");

        await careUnits.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
