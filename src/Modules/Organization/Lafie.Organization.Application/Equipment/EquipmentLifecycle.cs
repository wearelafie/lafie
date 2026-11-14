using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Equipment;

public sealed record ArchiveEquipmentCommand(Guid Id) : IRequest<Result>;

public sealed class ArchiveEquipmentHandler(IEquipmentRepository equipment) : IRequestHandler<ArchiveEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveEquipmentCommand command, CancellationToken cancellationToken)
    {
        var item = await equipment.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Équipement {command.Id} introuvable.");
        item.Archive();
        await equipment.UpdateAsync(item, cancellationToken);
        return Result.Success();
    }
}

public sealed record RestoreEquipmentCommand(Guid Id) : IRequest<Result>;

public sealed class RestoreEquipmentHandler(IEquipmentRepository equipment) : IRequestHandler<RestoreEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreEquipmentCommand command, CancellationToken cancellationToken)
    {
        var item = await equipment.FindAsync(command.Id, cancellationToken) ?? throw new NotFoundException($"Équipement {command.Id} introuvable.");
        item.Restore();
        await equipment.UpdateAsync(item, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Supprime définitivement un équipement.</summary>
public sealed record DeleteEquipmentCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteEquipmentHandler(IEquipmentRepository equipment) : IRequestHandler<DeleteEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteEquipmentCommand command, CancellationToken cancellationToken)
    {
        if (await equipment.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Équipement {command.Id} introuvable.");

        await equipment.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
