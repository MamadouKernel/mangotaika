using System.Security.Claims;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public sealed class PermissionService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ActiveRoleService activeRoleService) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode) || user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (await HasActualRoleAsync(user, RoleNames.Administrateur))
        {
            return true;
        }

        var permissions = await GetPermissionCodesAsync(user);
        return permissions.Contains(permissionCode);
    }

    public async Task<IReadOnlySet<string>> GetPermissionCodesAsync(ClaimsPrincipal user)
    {
        var roleNames = await ResolveRoleNamesAsync(user);
        if (roleNames.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var permissions = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Permission.IsActive && roleNames.Contains(rp.Role.Name!))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        return permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<string>> ResolveRoleNamesAsync(ClaimsPrincipal user)
    {
        var activeRole = activeRoleService.GetActiveRole(user);
        if (!string.IsNullOrWhiteSpace(activeRole))
        {
            return [activeRole];
        }

        var appUser = await userManager.GetUserAsync(user);
        if (appUser is null)
        {
            return [];
        }

        return (await userManager.GetRolesAsync(appUser)).ToList();
    }

    private async Task<bool> HasActualRoleAsync(ClaimsPrincipal user, string roleName)
    {
        var appUser = await userManager.GetUserAsync(user);
        return appUser is not null && await userManager.IsInRoleAsync(appUser, roleName);
    }
}
