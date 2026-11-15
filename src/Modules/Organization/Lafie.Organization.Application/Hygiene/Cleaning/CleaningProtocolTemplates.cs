using Lafie.Organization.Domain;

namespace Lafie.Organization.Application.Hygiene.Cleaning;

/// <summary>
/// Checklists par défaut semées à la création d'une tâche, selon le protocole adapté au risque.
/// Les étapes sont cumulatives (Deep = Standard + extensions, Terminal = Deep + extensions).
/// </summary>
public static class CleaningProtocolTemplates
{
    private static readonly string[] Standard =
    [
        "Aérer la pièce",
        "Évacuer les déchets et vider les poubelles",
        "Nettoyer les surfaces hautes et le mobilier",
        "Désinfecter les points de contact (poignées, interrupteurs, barrières)",
        "Nettoyer et désinfecter les sanitaires",
        "Laver et désinfecter le sol",
    ];

    private static readonly string[] DeepExtra =
    [
        "Nettoyer sous et derrière le mobilier",
        "Désinfecter le lit, le matelas et la literie",
        "Désinfecter les équipements médicaux fixes de la chambre",
        "Renouveler les consommables à usage unique",
    ];

    private static readonly string[] TerminalExtra =
    [
        "Désinfection complète des murs et surfaces verticales",
        "Changer rideaux, séparateurs et textiles",
        "Appliquer un produit virucide à large spectre",
        "Désinfection vapeur / UV si disponible",
        "Tracer les produits et la validation finale",
    ];

    /// <summary>Retourne les libellés d'étapes, dans l'ordre, pour le protocole demandé.</summary>
    public static IReadOnlyList<string> For(CleaningProtocol protocol) => protocol switch
    {
        CleaningProtocol.Standard => Standard,
        CleaningProtocol.Deep => [.. Standard, .. DeepExtra],
        CleaningProtocol.Terminal => [.. Standard, .. DeepExtra, .. TerminalExtra],
        _ => Standard,
    };
}
