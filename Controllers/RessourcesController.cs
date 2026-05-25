using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
public class RessourcesController(
    AppDbContext db,
    OperationalAccessService operationalAccessService,
    ActiveRoleService activeRoleService) : Controller
{
    private async Task<Guid?> GetManagedGroupIdAsync()
    {
        if (!User.IsInRole("ChefGroupe") || operationalAccessService.IsAdminLike(User)) return null;

        var activeRole = activeRoleService.GetActiveRole(User);
        if (activeRole != "ChefGroupe" && !User.IsInRole("ChefGroupe")) return null;

        var (groupeId, _) = await operationalAccessService.GetScopeAsync(User, "ChefGroupe");
        return groupeId;
    }

    private async Task<bool> HasManagedGroupAccessAsync()
    {
        if (!User.IsInRole("ChefGroupe") || operationalAccessService.IsAdminLike(User)) return true;
        return (await GetManagedGroupIdAsync()).HasValue;
    }

    private async Task LoadGroupsAsync(Guid? selectedGroupId = null)
    {
        var managedGroupId = await GetManagedGroupIdAsync();
        var query = db.Groupes.Where(g => g.IsActive);
        if (managedGroupId.HasValue)
        {
            query = query.Where(g => g.Id == managedGroupId.Value);
        }

        ViewBag.Groupes = await query.OrderBy(g => g.Nom).ToListAsync();
        ViewBag.ManagedGroupId = managedGroupId;
        ViewBag.SelectedGroupId = selectedGroupId ?? managedGroupId;
    }

    public async Task<IActionResult> Index(Guid? groupeId = null, string? q = null, TypeRessource? type = null)
    {
        var managedGroupId = await GetManagedGroupIdAsync();
        if (!await HasManagedGroupAccessAsync()) return Forbid();
        if (managedGroupId.HasValue) groupeId = managedGroupId.Value;

        var query = db.Ressources.Include(r => r.Groupe).AsQueryable();
        if (groupeId.HasValue) query = query.Where(r => r.GroupeId == groupeId.Value);
        if (type.HasValue) query = query.Where(r => r.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(r =>
                r.Nom.ToLower().Contains(term)
                || (r.Prenom != null && r.Prenom.ToLower().Contains(term))
                || (r.Telephone != null && r.Telephone.Contains(term))
                || (r.Email != null && r.Email.ToLower().Contains(term)));
        }

        await LoadGroupsAsync(groupeId);
        ViewBag.Search = q;
        ViewBag.Type = type;
        return View(await query.OrderBy(r => r.Groupe!.Nom).ThenBy(r => r.Nom).ThenBy(r => r.Prenom).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        if (!await HasManagedGroupAccessAsync()) return Forbid();
        await LoadGroupsAsync();
        return View("Upsert", new Ressource { GroupeId = ViewBag.SelectedGroupId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Ressource model)
    {
        var managedGroupId = await GetManagedGroupIdAsync();
        if (!await HasManagedGroupAccessAsync()) return Forbid();
        if (managedGroupId.HasValue) model.GroupeId = managedGroupId.Value;

        await ValidateGroupAsync(model.GroupeId);
        if (!ModelState.IsValid)
        {
            await LoadGroupsAsync(model.GroupeId);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.Nom = model.Nom.Trim();
        model.Prenom = string.IsNullOrWhiteSpace(model.Prenom) ? null : model.Prenom.Trim();
        db.Ressources.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Ressource creee.";
        return RedirectToAction(nameof(Index), new { groupeId = model.GroupeId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await db.Ressources.FindAsync(id);
        if (model is null) return NotFound();
        var managedGroupId = await GetManagedGroupIdAsync();
        if (!await HasManagedGroupAccessAsync()) return Forbid();
        if (managedGroupId.HasValue && model.GroupeId != managedGroupId.Value) return Forbid();
        await LoadGroupsAsync(model.GroupeId);
        return View("Upsert", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Ressource model)
    {
        var existing = await db.Ressources.FindAsync(id);
        if (existing is null) return NotFound();
        var managedGroupId = await GetManagedGroupIdAsync();
        if (!await HasManagedGroupAccessAsync()) return Forbid();
        if (managedGroupId.HasValue && existing.GroupeId != managedGroupId.Value) return Forbid();

        await ValidateGroupAsync(managedGroupId ?? model.GroupeId);
        if (!ModelState.IsValid)
        {
            await LoadGroupsAsync(model.GroupeId);
            return View("Upsert", model);
        }

        existing.Nom = model.Nom.Trim();
        existing.Prenom = string.IsNullOrWhiteSpace(model.Prenom) ? null : model.Prenom.Trim();
        existing.Telephone = string.IsNullOrWhiteSpace(model.Telephone) ? null : model.Telephone.Trim();
        existing.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        existing.Type = model.Type;
        existing.GroupeId = managedGroupId ?? model.GroupeId;
        existing.Notes = model.Notes;
        existing.IsActive = model.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = "Ressource mise a jour.";
        return RedirectToAction(nameof(Index), new { groupeId = existing.GroupeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await db.Ressources.FindAsync(id);
        if (existing is null) return NotFound();
        var managedGroupId = await GetManagedGroupIdAsync();
        if (!await HasManagedGroupAccessAsync()) return Forbid();
        if (managedGroupId.HasValue && existing.GroupeId != managedGroupId.Value) return Forbid();
        existing.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Ressource desactivee.";
        return RedirectToAction(nameof(Index), new { groupeId = existing.GroupeId });
    }

    private async Task ValidateGroupAsync(Guid? groupeId)
    {
        if (!groupeId.HasValue)
        {
            ModelState.AddModelError(nameof(Ressource.GroupeId), "Selectionnez le groupe de rattachement.");
            return;
        }

        var exists = await db.Groupes.AnyAsync(g => g.Id == groupeId.Value && g.IsActive);
        if (!exists)
        {
            ModelState.AddModelError(nameof(Ressource.GroupeId), "Le groupe selectionne est invalide.");
        }
    }
}
