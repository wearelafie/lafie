using Lafie.Core.Common;
using Lafie.Organization.Application.References;

namespace Lafie.Organization.Application.Organizations;

/// <summary>Valide que les codes d'un établissement existent dans les référentiels.</summary>
internal static class ReferenceValidation
{
    public static async Task<Result> ValidateAsync(IReferenceDataRepository reference, OrganizationWriteModel m, CancellationToken ct)
    {
        if (!await reference.ReferenceValueExistsAsync("FACILITY_TYPE", m.FacilityTypeCode, ct))
            return Result.Failure($"Type d'établissement inconnu : '{m.FacilityTypeCode}'.");
        if (!await reference.ReferenceValueExistsAsync("OWNERSHIP_TYPE", m.OwnershipTypeCode, ct))
            return Result.Failure($"Type de propriété inconnu : '{m.OwnershipTypeCode}'.");

        if (!string.IsNullOrWhiteSpace(m.LegalEntityTypeCode)
            && !await reference.ReferenceValueExistsAsync("LEGAL_ENTITY_TYPE", m.LegalEntityTypeCode, ct))
            return Result.Failure($"Forme juridique inconnue : '{m.LegalEntityTypeCode}'.");
        if (!string.IsNullOrWhiteSpace(m.AccreditationStatusCode)
            && !await reference.ReferenceValueExistsAsync("ACCREDITATION_STATUS", m.AccreditationStatusCode, ct))
            return Result.Failure($"Statut d'accréditation inconnu : '{m.AccreditationStatusCode}'.");

        return Result.Success();
    }
}
