using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.CareUnits;
using Mediator;

namespace Lafie.Organization.Application.Services;

public sealed record ArchiveServiceCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveServiceHandler(IServiceRepository services) : IRequestHandler<ArchiveServiceCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveServiceCommand command, CancellationToken cancellationToken)
    {
        var s = await services.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Service {command.Id} introuvable.");
        s.Archive();
        await services.UpdateAsync(s, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreServiceCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreServiceHandler(IServiceRepository services) : IRequestHandler<RestoreServiceCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreServiceCommand command, CancellationToken cancellationToken)
    {
        var s = await services.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Service {command.Id} introuvable.");
        s.Restore();
        await services.UpdateAsync(s, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement un service (bloqué s'il a des sous-services ou des unités de soins).</summary>
public sealed record DeleteServiceCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteServiceHandler(IServiceRepository services, ICareUnitRepository careUnits)
    : IRequestHandler<DeleteServiceCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteServiceCommand command, CancellationToken cancellationToken)
    {
        if (await services.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Service {command.Id} introuvable.");

        if (await services.HasChildrenAsync(command.Id, cancellationToken))
            return Result.Failure("Suppression impossible : le service a des sous-services.");

        var units = await careUnits.SearchByServiceAsync(command.Id, new CareUnitFilter(IncludeArchived: true, PageSize: 1), cancellationToken);
        if (units.Total > 0)
            return Result.Failure("Suppression impossible : le service contient des unités de soins.");

        await services.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
