using System.Security.Claims;

namespace MangoTaika.Services;

public static class RoleNames
{
    public const string Administrateur = "Administrateur";
    public const string CommissaireDistrict = "CommissaireDistrict";
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
        Administrateur, CommissaireDistrict, Gestionnaire, AgentSupport,
        Superviseur, Consultant,
        EquipeDistrict, ChefGroupe, ChefUnite,
        Scout, Parent
    ];

    public static bool IsAdminLike(ClaimsPrincipal user)
        => user.IsInRole(Administrateur) || user.IsInRole(CommissaireDistrict);

    public static bool IsAdminRole(string role)
        => role is Administrateur or CommissaireDistrict;
}
