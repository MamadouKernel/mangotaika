using System.Security.Claims;

namespace MangoTaika.Services;

public class ActiveRoleService(IHttpContextAccessor httpContextAccessor)
{
    private const string SessionKey = "ActiveRole";

    public static readonly string[] AllRoles = RoleNames.All;

    private static readonly Dictionary<string, string> RoleLabels = new()
    {
        ["Administrateur"]      = "Administrateur",
        ["CommissaireDistrict"] = "Commissaire de District",
        ["Gestionnaire"]        = "Gestionnaire",
        ["AgentSupport"]        = "Agent de support",
        ["Superviseur"]         = "Superviseur",
        ["Consultant"]          = "Consultant",
        ["EquipeDistrict"]      = "Equipe de District",
        ["ChefGroupe"]          = "Chef de Groupe",
        ["ChefUnite"]           = "Chef d'Unite",
        ["Scout"]               = "Scout",
        ["Parent"]              = "Parent / Tuteur"
    };

    public string? GetActiveRole(ClaimsPrincipal user)
    {
        var session = httpContextAccessor.HttpContext?.Session;
        var roles = GetUserRoles(user);
        if (session is not null)
        {
            var stored = session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(stored) && roles.Contains(stored))
                return stored;
        }
        return roles.FirstOrDefault();
    }

    public void SetActiveRole(string role)
    {
        httpContextAccessor.HttpContext?.Session.SetString(SessionKey, role);
    }

    public static List<string> GetUserRoles(ClaimsPrincipal user)
    {
        var roles = AllRoles.Where(user.IsInRole).ToList();
        if (CommissaireDistrictClaimsTransformation.HasAdministrateurAlias(user))
        {
            roles.Remove(RoleNames.Administrateur);
        }

        return roles;
    }

    public static string GetLabel(string role)
        => RoleLabels.TryGetValue(role, out var label) ? label : role;
}
