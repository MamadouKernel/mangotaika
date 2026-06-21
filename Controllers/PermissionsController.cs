using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
public class PermissionsController(
    AppDbContext db,
    RoleManager<IdentityRole<Guid>> roleManager) : Controller
{
    public async Task<IActionResult> Index(Guid? roleId)
    {
        var roles = await roleManager.Roles
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync();

        var selectedRoleId = roleId ?? roles.FirstOrDefault()?.Id;
        var selectedRole = selectedRoleId.HasValue
            ? roles.FirstOrDefault(r => r.Id == selectedRoleId.Value)
            : null;

        var selectedPermissions = selectedRole is null
            ? new HashSet<Guid>()
            : await db.RolePermissions
                .Where(rp => rp.RoleId == selectedRole.Id)
                .Select(rp => rp.PermissionId)
                .ToHashSetAsync();

        ViewBag.Roles = roles;
        ViewBag.SelectedRoleId = selectedRoleId;
        ViewBag.SelectedRoleName = selectedRole?.Name ?? "Aucun role";
        ViewBag.SelectedPermissions = selectedPermissions;

        var permissions = await db.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Libelle)
            .AsNoTracking()
            .ToListAsync();

        return View(permissions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(Guid roleId, List<Guid> permissionIds)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            TempData["Error"] = "Role introuvable. Les permissions n'ont pas ete modifiees.";
            return RedirectToAction(nameof(Index));
        }

        var allowedPermissionIds = await db.Permissions
            .Where(p => p.IsActive && permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var current = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync();

        db.RolePermissions.RemoveRange(current);

        foreach (var permissionId in allowedPermissionIds.Distinct())
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"Permissions mises a jour pour le role {role.Name}.";
        return RedirectToAction(nameof(Index), new { roleId = role.Id });
    }
}
