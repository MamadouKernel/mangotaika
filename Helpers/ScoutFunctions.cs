namespace MangoTaika.Helpers;

/// <summary>
/// Liste canonique des fonctions scoutes proposees dans les formulaires (creation/edition).
/// Centralisee ici pour eviter toute divergence entre les vues. L'ordre suit la hierarchie :
/// branches, puis unite, groupe et district (titulaire, adjoint, assistant).
/// </summary>
public static class ScoutFunctions
{
    public static readonly IReadOnlyList<string> All =
    [
        "OISILLON (4 - 7 ANS)",
        "LOUVETEAU (8 - 11 ANS)",
        "ECLAIREUR (12 - 14 ANS)",
        "CHEMINOT (15 - 17 ANS)",
        "ROUTIER (18 - 21 ANS)",
        "BENEVOLES (+ de 21 ANS)",
        "CHEF D'UNITE (CU)",
        "CHEF D'UNITE ADJOINT (CUA)",
        "ASSISTANT CHEF D'UNITE (ACU)",
        "CHEF DE GROUPE (CG)",
        "CHEF DE GROUPE ADJOINT (CGA)",
        "ASSISTANT CHEF DE GROUPE (ACG)",
        "COMMISSAIRE DE DISTRICT (CD)",
        "COMMISSAIRE DE DISTRICT ADJOINT (CDA)",
        "ASSISTANT COMMISSAIRE DE DISTRICT (ACD)"
    ];
}
