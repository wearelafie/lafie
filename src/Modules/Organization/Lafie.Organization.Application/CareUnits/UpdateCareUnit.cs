using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Domain;
using Mediator;

namespace Lafie.Organization.Application.CareUnits;

/// <summary>Met à jour une unité de soins.</summary>
public sealed record UpdateCareUnitCommand(
    Guid Id, string Code, string Name, string Type,
    int? BedCapacity = null, string? Description = null, int DisplayOrder = 0)
    : IRequest<Result>;

public sealed class UpdateCareUnitHandler(ICareUnitRepository careUnits)
    : IRequestHandler<UpdateCareUnitCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateCareUnitCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CareUnitType>(command.Type, ignoreCase: true, out var type))
            return Result.Failure($"Type d'unité inconnu : '{command.Type}'.");

        var unit = await careUnits.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Unité de soins {command.Id} introuvable.");

        var updated = unit.Update(command.Code, command.Name, type, command.BedCapacity, command.Description, command.DisplayOrder);
        if (updated.IsFailure) return updated;

        if (await careUnits.CodeExistsAsync(unit.FacilityId, unit.Code, unit.Id, cancellationToken))
            return Result.Failure($"Le code '{unit.Code}' est déjà utilisé dans cet établissement.");

        await careUnits.UpdateAsync(unit, cancellationToken);
        return Result.Success();
    }
}
