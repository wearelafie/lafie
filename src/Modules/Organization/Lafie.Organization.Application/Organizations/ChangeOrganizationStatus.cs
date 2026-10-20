using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Désactive une organisation (désactivation logique → statut Inactive).</summary>
public sealed record DeactivateOrganizationCommand(Guid Id) : IRequest<Result>;

public sealed class DeactivateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<DeactivateOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(DeactivateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Organisation {command.Id} introuvable.");
        org.Deactivate();
        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Réactive une organisation (statut Active).</summary>
public sealed record ActivateOrganizationCommand(Guid Id) : IRequest<Result>;

public sealed class ActivateOrganizationHandler(IOrganizationRepository repository)
    : IRequestHandler<ActivateOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(ActivateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Organisation {command.Id} introuvable.");
        org.Activate();
        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}
