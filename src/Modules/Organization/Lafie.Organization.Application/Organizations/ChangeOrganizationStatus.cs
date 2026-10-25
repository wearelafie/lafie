using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Change l'état opérationnel d'un établissement (fermeture logique, suspension, réouverture…).</summary>
public sealed record ChangeOrganizationStatusCommand(Guid Id, string OperationalStatus) : IRequest<Result>;

public sealed class ChangeOrganizationStatusHandler(IOrganizationRepository repository)
    : IRequestHandler<ChangeOrganizationStatusCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeOrganizationStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OperationalStatus>(command.OperationalStatus, ignoreCase: true, out var status))
            return Result.Failure($"Statut opérationnel inconnu : '{command.OperationalStatus}'.");

        var org = await repository.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Établissement {command.Id} introuvable.");

        org.ChangeOperationalStatus(status);
        await repository.UpdateAsync(org, cancellationToken);
        return Result.Success();
    }
}
