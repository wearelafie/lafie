using Lafie.Core.Common;
using Lafie.Organization.Application.CareUnits;
using Lafie.Organization.Application.References;
using Lafie.Organization.Application.Services;
using Lafie.Organization.Application.Spaces;

namespace Lafie.Organization.Application.Equipment;

/// <summary>
/// Valide la catégorie (référentiel EQUIPMENT_CATEGORY) et l'appartenance à
/// l'établissement de la localisation (espace) et de l'affectation (service / unité).
/// </summary>
internal static class EquipmentValidation
{
    public const string CategoryReference = "EQUIPMENT_CATEGORY";

    public static async Task<Result> ValidateAsync(
        IReferenceDataRepository reference, ISpaceRepository spaces, IServiceRepository services,
        ICareUnitRepository careUnits, Guid facilityId, string category,
        Guid? spaceId, Guid? serviceId, Guid? careUnitId, CancellationToken ct)
    {
        if (!await reference.ReferenceValueExistsAsync(CategoryReference, category, ct))
            return Result.Failure($"Catégorie d'équipement inconnue : '{category}'.");

        if (spaceId is { } sp && sp != Guid.Empty)
        {
            var space = await spaces.GetByIdAsync(sp, ct);
            if (space is null) return Result.Failure("Localisation (espace) introuvable.");
            if (space.FacilityId != facilityId)
                return Result.Failure("La localisation appartient à un autre établissement.");
        }

        if (serviceId is { } svc && svc != Guid.Empty)
        {
            var service = await services.GetByIdAsync(svc, ct);
            if (service is null) return Result.Failure("Service d'affectation introuvable.");
            if (service.FacilityId != facilityId)
                return Result.Failure("Le service d'affectation appartient à un autre établissement.");
        }

        if (careUnitId is { } cu && cu != Guid.Empty)
        {
            var careUnit = await careUnits.GetByIdAsync(cu, ct);
            if (careUnit is null) return Result.Failure("Unité de soins d'affectation introuvable.");
            if (careUnit.FacilityId != facilityId)
                return Result.Failure("L'unité de soins appartient à un autre établissement.");
            if (serviceId is { } svc2 && svc2 != Guid.Empty && careUnit.ServiceId != svc2)
                return Result.Failure("L'unité de soins n'appartient pas au service d'affectation.");
        }

        return Result.Success();
    }
}
