using Lafie.Core.Common;
using Lafie.Core.Exceptions;
using Lafie.Organization.Application.CareUnits;
using Lafie.Organization.Application.References;
using Lafie.Organization.Application.Services;
using Lafie.Organization.Application.Spaces;
using Mediator;

namespace Lafie.Organization.Application.Equipment;

/// <summary>Met à jour un équipement (hors changement d'établissement et d'état).</summary>
public sealed record UpdateEquipmentCommand(
    Guid Id, string InventoryNumber, string? SerialNumber, string Name, string Category,
    string? Brand, string? Model, DateOnly? AcquisitionDate, string? Supplier, DateOnly? WarrantyEnd,
    int? EstimatedLifespanMonths, Guid? SpaceId, Guid? ServiceId, Guid? CareUnitId,
    string? Notes = null, int DisplayOrder = 0)
    : IRequest<Result>;

public sealed class UpdateEquipmentHandler(
    IEquipmentRepository equipment,
    IReferenceDataRepository reference,
    ISpaceRepository spaces,
    IServiceRepository services,
    ICareUnitRepository careUnits) : IRequestHandler<UpdateEquipmentCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var item = await equipment.FindAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Équipement {command.Id} introuvable.");

        var validation = await EquipmentValidation.ValidateAsync(
            reference, spaces, services, careUnits, item.FacilityId, command.Category,
            command.SpaceId, command.ServiceId, command.CareUnitId, cancellationToken);
        if (validation.IsFailure) return validation;

        var updated = item.Update(
            command.InventoryNumber, command.SerialNumber, command.Name, command.Category,
            command.Brand, command.Model, command.AcquisitionDate, command.Supplier, command.WarrantyEnd,
            command.EstimatedLifespanMonths, command.SpaceId, command.ServiceId, command.CareUnitId,
            command.Notes, command.DisplayOrder);
        if (updated.IsFailure) return updated;

        if (await equipment.InventoryNumberExistsAsync(item.FacilityId, item.InventoryNumber, item.Id, cancellationToken))
            return Result.Failure($"Le numéro d'inventaire '{item.InventoryNumber}' est déjà utilisé dans cet établissement.");

        await equipment.UpdateAsync(item, cancellationToken);
        return Result.Success();
    }
}
