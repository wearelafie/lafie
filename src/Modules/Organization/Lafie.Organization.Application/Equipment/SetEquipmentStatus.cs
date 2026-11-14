using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.Equipment;

/// <summary>Change l'état opérationnel d'un équipement.</summary>
public sealed record SetEquipmentStatusCommand(Guid Id, string Status) : IRequest<Result>;

public sealed class SetEquipmentStatusHandler(IEquipmentRepository equipment)
    : IRequestHandler<SetEquipmentStatusCommand, Result>
{
    public async ValueTask<Result> Handle(SetEquipmentStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EquipmentStatus>(command.Status, ignoreCase: true, out var status))
            return Result.Failure($"État inconnu : '{command.Status}'.");

        var item = await equipment.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Équipement {command.Id} introuvable.");

        item.SetStatus(status);
        await equipment.UpdateAsync(item, cancellationToken);
        return Result.Success();
    }
}
