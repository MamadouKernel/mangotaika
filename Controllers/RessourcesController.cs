using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
public class RessourcesController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<Guid?> GetManagedGroupIdAsync()
    {
        if (!User.IsInRole("ChefGroupe")) return null;
        var user = await userManager.GetUserAsync(User);
        return user?.GroupeId;
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
        await LoadGroupsAsync();
        return View("Upsert", new Ressource { GroupeId = ViewBag.SelectedGroupId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Ressource model)
    {
        var managedGroupId = await GetManagedGroupIdAsync();
        if (managedGroupId.HasValue) model.GroupeId = managedGroupId.Value;

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
        if (managedGroupId.HasValue && existing.GroupeId != managedGroupId.Value) return Forbid();

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
        if (managedGroupId.HasValue && existing.GroupeId != managedGroupId.Value) return Forbid();
        existing.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Ressource desactivee.";
        return RedirectToAction(nameof(Index), new { groupeId = existing.GroupeId });
    }
}
