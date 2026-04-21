using System.Security.Claims;

namespace MangoTaika.Services;

public class ActiveRoleService(IHttpContextAccessor httpContextAccessor)
{
    private const string SessionKey = "ActiveRole";

    public static readonly string[] AllRoles =
    [
        "Administrateur", "Gestionnaire", "AgentSupport",
        "Superviseur", "Consultant",
        "AssistantCommissaire", "ChefGroupe", "ChefUnite",
        "Scout", "Parent"
    ];

    private static readonly Dictionary<string, string> RoleLabels = new()
    {
        ["Administrateur"]      = "Administrateur",
        ["Gestionnaire"]        = "Gestionnaire",
        ["AgentSupport"]        = "Agent de support",
        ["Superviseur"]         = "Superviseur",
        ["Consultant"]          = "Consultant",
        ["AssistantCommissaire"] = "Assistant Commissaire",
        ["ChefGroupe"]          = "Chef de Groupe",
        ["ChefUnite"]           = "Chef d'Unite",
        ["Scout"]               = "Scout",
        ["Parent"]              = "Parent / Tuteur"
    };

    public string? GetActiveRole(ClaimsPrincipal user)
    {
        var session = httpContextAccessor.HttpContext?.Session;
        if (session is not null)
        {
            var stored = session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(stored) && user.IsInRole(stored))
                return stored;
        }
        return AllRoles.FirstOrDefault(user.IsInRole);
    }

    public void SetActiveRole(string role)
    {
        httpContextAccessor.HttpContext?.Session.SetString(SessionKey, role);
    }

    public static List<string> GetUserRoles(ClaimsPrincipal user)
        => AllRoles.Where(user.IsInRole).ToList();

    public static string GetLabel(string role)
        => RoleLabels.TryGetValue(role, out var label) ? label : role;
}
