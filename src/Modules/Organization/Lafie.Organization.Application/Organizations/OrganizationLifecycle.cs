using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.Buildings;
using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Archive (soft-delete) un établissement.</summary>
public sealed record ArchiveOrganizationCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveOrganizationHandler(IOrganizationRepository repository) : IRequestHandler<ArchiveOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveOrganizationCommand command, CancellationToken cancellationToken)
    {
        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Établissement {command.Id} introuvable.");
        org.Archive();
        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Restaure un établissement archivé.</summary>
public sealed record RestoreOrganizationCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreOrganizationHandler(IOrganizationRepository repository) : IRequestHandler<RestoreOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreOrganizationCommand command, CancellationToken cancellationToken)
    {
        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Établissement {command.Id} introuvable.");
        org.Restore();
        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement un établissement (bloqué s'il contient des bâtiments).</summary>
public sealed record DeleteOrganizationCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteOrganizationHandler(
    IOrganizationRepository repository,
    IBuildingRepository buildings) : IRequestHandler<DeleteOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteOrganizationCommand command, CancellationToken cancellationToken)
    {
        if (await repository.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Établissement {command.Id} introuvable.");

        var children = await buildings.SearchAsync(command.Id, new BuildingFilter(IncludeArchived: true, PageSize: 1), cancellationToken);
        if (children.Total > 0)
            return Result.Failure("Suppression impossible : l'établissement contient des bâtiments. Supprimez-les d'abord ou archivez l'établissement.");

        await repository.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
