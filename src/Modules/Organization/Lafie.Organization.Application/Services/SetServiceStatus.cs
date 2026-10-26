using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Services;

/// <summary>Change l'état d'un service.</summary>
public sealed record SetServiceStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetServiceStatusHandler(IServiceRepository services)
    : IRequestHandler<SetServiceStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetServiceStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ServiceStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"Statut inconnu : '{command.Status}'.");

        var service = await services.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Service {command.Id} introuvable.");

        service.SetStatus(status);
        await services.UpdateAsync(service, cancellationToken);
        return Result.Success();
    }
}
