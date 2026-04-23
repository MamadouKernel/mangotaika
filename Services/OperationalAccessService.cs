using System.Security.Claims;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public sealed class OperationalAccessService(AppDbContext db, UserManager<ApplicationUser> userManager)
{
    public bool IsAdminLike(ClaimsPrincipal user)
        => RoleNames.IsAdminLike(user) || user.IsInRole(RoleNames.Gestionnaire);

    public bool IsSupervision(ClaimsPrincipal user)
        => user.IsInRole("Superviseur") || user.IsInRole("Consultant");

    public bool IsEquipeDistrict(ClaimsPrincipal user) => user.IsInRole(RoleNames.EquipeDistrict);
    public bool IsChefGroupe(ClaimsPrincipal user) => user.IsInRole("ChefGroupe");
    public bool IsChefUnite(ClaimsPrincipal user) => user.IsInRole("ChefUnite");
    public bool IsScopedLeader(ClaimsPrincipal user)
        => IsEquipeDistrict(user) || IsChefGroupe(user) || IsChefUnite(user);

    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var userId = userManager.GetUserId(user);
        if (!Guid.TryParse(userId, out var parsed)) return null;
        return await db.Users.FindAsync(parsed);
    }

    public async Task<(Guid? GroupeId, Guid? BrancheId)> GetScopeAsync(ClaimsPrincipal user, string activeRole)
    {
        var appUser = await GetCurrentUserAsync(user);
        if (appUser is null) return (null, null);
        return activeRole switch
        {
            "ChefGroupe"            => (appUser.GroupeId, null),
            "EquipeDistrict"        => (null, appUser.BrancheId),
            "ChefUnite"             => (appUser.GroupeId, appUser.BrancheId),
            _                       => (null, null)
        };
    }

    public async Task<Scout?> GetCurrentScoutAsync(ClaimsPrincipal user)
    {
        var userId = userManager.GetUserId(user);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        return await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.UserId == parsedUserId && s.IsActive);
    }

    public async Task<bool> IsLeadershipScoutAsync(ClaimsPrincipal user)
    {
        var scout = await GetCurrentScoutAsync(user);
        return IsLeadershipFunction(scout?.Fonction);
    }

    public async Task<bool> IsDistrictReviewerAsync(ClaimsPrincipal user)
    {
        var scout = await GetCurrentScoutAsync(user);
        return scout is not null
            && IsDistrictEquipe(scout.Groupe?.Nom)
            && IsDistrictValidationFunction(scout.Fonction);
    }

    public async Task<bool> CanManageGroupeAsync(ClaimsPrincipal user, Guid? groupeId)
    {
        if (IsAdminLike(user))
        {
            return true;
        }

        var scout = await GetCurrentScoutAsync(user);
        return scout is not null
            && IsLeadershipFunction(scout.Fonction)
            && scout.GroupeId == groupeId;
    }

    public static bool IsDistrictEquipe(string? groupeNom)
    {
        return DatabaseText.NormalizeSearchKey(groupeNom) == DatabaseText.NormalizeSearchKey("Equipe de District Mango Taika");
    }

    public static bool IsLeadershipFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction);
        return normalizedFunction.Contains("CHEF", StringComparison.Ordinal)
            || normalizedFunction.Contains("COMMISSAIRE", StringComparison.Ordinal)
            || normalizedFunction.Contains("RESPONSABLE", StringComparison.Ordinal);
    }

    public static bool IsDistrictValidationFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction);
        return normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT (CD)")
            || normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT ADJOINT (CDA)")
            || normalizedFunction == DatabaseText.NormalizeSearchKey("ASSISTANT COMMISSAIRE DE DISTRICT (ACD)");
    }
}
