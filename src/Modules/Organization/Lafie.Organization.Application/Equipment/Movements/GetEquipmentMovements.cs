using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Mediator;

namespace Lafie.Organization.Application.Equipment.Movements;

/// <summary>Liste paginée des déplacements d'un équipement (plus récents d'abord).</summary>
public sealed record GetEquipmentMovementsQuery(Guid EquipmentId, EquipmentMovementFilter Filter)
    : IRequest<PagedResult<EquipmentMovementDto>>;

public sealed class GetEquipmentMovementsHandler(IEquipmentMovementRepository movements)
    : IRequestHandler<GetEquipmentMovementsQuery, PagedResult<EquipmentMovementDto>>
{
    public async ValueTask<PagedResult<EquipmentMovementDto>> Handle(GetEquipmentMovementsQuery query, CancellationToken cancellationToken)
        => await movements.SearchByEquipmentAsync(query.EquipmentId, query.Filter, cancellationToken);
}

/// <summary>Supprime une entrée du journal des déplacements.</summary>
public sealed record DeleteEquipmentMovementCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteEquipmentMovementHandler(IEquipmentMovementRepository movements)
    : IRequestHandler<DeleteEquipmentMovementCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteEquipmentMovementCommand command, CancellationToken cancellationToken)
    {
        if (await movements.GetByIdAsync(command.Id, cancellationToken) is null)
            throw new NotFoundException($"Déplacement {command.Id} introuvable.");

        await movements.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}
