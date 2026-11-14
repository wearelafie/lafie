using Lafie.Core.Common;
using Lafie.Organization.Application.CareUnits;
using Lafie.Organization.Application.References;
using Lafie.Organization.Application.Services;
using Lafie.Organization.Application.Spaces;
using Lafie.Organization.Domain;
using Mediator;
using DomainEquipment = Lafie.Organization.Domain.Equipment;

namespace Lafie.Organization.Application.Equipment;

/// <summary>Enregistre un équipement dans un établissement.</summary>
public sealed record CreateEquipmentCommand(
    Guid FacilityId, string InventoryNumber, string? SerialNumber, string Name, string Category,
    string? Brand, string? Model, DateOnly? AcquisitionDate, string? Supplier, DateOnly? WarrantyEnd,
    int? EstimatedLifespanMonths, Guid? SpaceId, Guid? ServiceId, Guid? CareUnitId,
    string? Status = null, string? Notes = null, int DisplayOrder = 0)
    : IRequest<Result<Guid>>;

public sealed class CreateEquipmentHandler(
    IEquipmentRepository equipment,
    IOrganizationRepository facilities,
    IReferenceDataRepository reference,
    ISpaceRepository spaces,
    IServiceRepository services,
    ICareUnitRepository careUnits) : IRequestHandler<CreateEquipmentCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var status = EquipmentStatus.Operational;
        if (!string.IsNullOrWhiteSpace(command.Status)
            && !Enum.TryParse(command.Status, ignoreCase: true, out status))
            return Result.Failure<Guid>($"État d'équipement inconnu : '{command.Status}'.");

        if (await facilities.GetByIdAsync(command.FacilityId, cancellationToken) is null)
            return Result.Failure<Guid>("Établissement introuvable.");

        var validation = await EquipmentValidation.ValidateAsync(
            reference, spaces, services, careUnits, command.FacilityId, command.Category,
            command.SpaceId, command.ServiceId, command.CareUnitId, cancellationToken);
        if (validation.IsFailure) return Result.Failure<Guid>(validation.Error!);

        var created = DomainEquipment.Create(
            command.FacilityId, command.InventoryNumber, command.SerialNumber, command.Name, command.Category,
            command.Brand, command.Model, command.AcquisitionDate, command.Supplier, command.WarrantyEnd,
            command.EstimatedLifespanMonths, command.SpaceId, command.ServiceId, command.CareUnitId,
            status, command.Notes, command.DisplayOrder);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error!);

        if (await equipment.InventoryNumberExistsAsync(command.FacilityId, created.Value.InventoryNumber, null, cancellationToken))
            return Result.Failure<Guid>($"Le numéro d'inventaire '{created.Value.InventoryNumber}' est déjà utilisé dans cet établissement.");

        await equipment.AddAsync(created.Value, cancellationToken);
        return Result.Success(created.Value.Id);
    }
}
