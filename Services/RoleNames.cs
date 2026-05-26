using System.Security.Claims;

namespace MangoTaika.Services;

public static class RoleNames
{
    public const string Administrateur = "Administrateur";
    public const string CommissaireDistrict = "CommissaireDistrict";
    public const string CommissaireDistrictAdjoint = "CommissaireDistrictAdjoint";
    public const string AssistantCommissaireDistrict = "AssistantCommissaireDistrict";
    public const string Gestionnaire = "Gestionnaire";
    public const string AgentSupport = "AgentSupport";
    public const string Superviseur = "Superviseur";
    public const string Consultant = "Consultant";
    public const string EquipeDistrict = "EquipeDistrict";
    public const string ChefGroupe = "ChefGroupe";
    public const string ChefUnite = "ChefUnite";
    public const string Scout = "Scout";
    public const string Parent = "Parent";

    public static readonly string[] All =
    [
        Administrateur, CommissaireDistrict, CommissaireDistrictAdjoint, AssistantCommissaireDistrict,
        Gestionnaire, AgentSupport,
        Superviseur, Consultant,
        EquipeDistrict, ChefGroupe, ChefUnite,
        Scout, Parent
    ];

    public static readonly IReadOnlyList<RoleDefinition> Definitions =
    [
        new(Administrateur, "Administrateur", 0, "All", "Acces complet a la plateforme."),
        new(CommissaireDistrict, "Commissaire de District", 1, "Donnees du district", "Pilotage et validation district."),
        new(CommissaireDistrictAdjoint, "Commissaire de District Adjoint", 2, "Donnees du district", "Appui au commissaire de district."),
        new(AssistantCommissaireDistrict, "Assistant Commissaire de District", 3, "Donnees du district", "Suivi operationnel district."),
        new(ChefGroupe, "Chef de Groupe", 4, "Uniquement les donnees de son groupe", "Gestion operationnelle de son groupe."),
        new(ChefUnite, "Chef d'Unite", 5, "Uniquement les donnees de sa branche dans son groupe", "Gestion de sa branche."),
        new(Scout, "Scout", 6, "Uniquement ses donnees", "Consultation et suivi individuel."),
        new(Parent, "Parent / Tuteur", 7, "Uniquement les donnees de son enfant", "Suivi des enfants rattaches."),
        new(EquipeDistrict, "Equipe de District", 3, "Donnees du district selon fonction", "Appui aux activites district."),
        new(Gestionnaire, "Gestionnaire", 1, "Donnees du district", "Administration fonctionnelle."),
        new(AgentSupport, "Agent de support", 3, "Support et tickets", "Traitement du support."),
        new(Superviseur, "Superviseur", 2, "Lecture et supervision", "Controle et suivi."),
        new(Consultant, "Consultant", 2, "Lecture et supervision", "Consultation et analyse.")
    ];

    public static bool IsAdminLike(ClaimsPrincipal user)
        => user.IsInRole(Administrateur)
            || user.IsInRole(CommissaireDistrict)
            || user.IsInRole(CommissaireDistrictAdjoint)
            || user.IsInRole(AssistantCommissaireDistrict);

    public static bool IsAdminRole(string role)
        => role is Administrateur or CommissaireDistrict or CommissaireDistrictAdjoint or AssistantCommissaireDistrict;

    public static RoleDefinition GetDefinition(string role)
        => Definitions.FirstOrDefault(r => r.Name == role)
            ?? new RoleDefinition(role, role, 99, "Non defini", "Role non documente.");
}

public sealed record RoleDefinition(string Name, string Label, int Hierarchy, string Visibility, string Description);
