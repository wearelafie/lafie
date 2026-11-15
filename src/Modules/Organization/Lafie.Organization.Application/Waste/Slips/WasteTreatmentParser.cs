using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Waste.Slips;

/// <summary>Analyse d'un mode de traitement optionnel : vide/null = non renseigné (valide).</summary>
internal static class WasteTreatmentParser
{
    public static bool TryParse(string? value, out WasteTreatment? treatment)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            treatment = null;
            return true;
        }
        if (Enum.TryParse<WasteTreatment>(value, ignoreCase: true, out var parsed))
        {
            treatment = parsed;
            return true;
        }
        treatment = null;
        return false;
    }
}
