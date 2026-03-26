using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Superviseur,Consultant")]
public class SupportCatalogController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.SupportCatalogueServices
            .Include(s => s.Auteur)
            .OrderByDescending(s => s.EstActif)
            .ThenBy(s => s.Nom)
            .ToListAsync();
        ViewBag.CanManage = CanManage();
        return View(items);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var item = await db.SupportCatalogueServices
            .Include(s => s.Auteur)
            .Include(s => s.AssigneParDefaut)
            .Include(s => s.GroupeParDefaut)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        ViewBag.CanManage = CanManage();
        return View(item);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> Create()
    {
        await PopulateSupportTargetsAsync();
        return View(new SupportServiceCatalogueItem());
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupportServiceCatalogueItem model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSupportTargetsAsync();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.Code = model.Code.Trim().ToUpperInvariant();
        model.Nom = model.Nom.Trim();
        model.AuteurId = Guid.Parse(userManager.GetUserId(User)!);
        model.DateCreation = DateTime.UtcNow;
        db.SupportCatalogueServices.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Service du catalogue cree.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await db.SupportCatalogueServices.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        await PopulateSupportTargetsAsync();
        return View(item);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SupportServiceCatalogueItem model)
    {
        var item = await db.SupportCatalogueServices.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSupportTargetsAsync();
            return View(model);
        }

        item.Code = model.Code.Trim().ToUpperInvariant();
        item.Nom = model.Nom.Trim();
        item.Description = model.Description.Trim();
        item.TypeParDefaut = model.TypeParDefaut;
        item.CategorieParDefaut = model.CategorieParDefaut;
        item.ImpactParDefaut = model.ImpactParDefaut;
        item.UrgenceParDefaut = model.UrgenceParDefaut;
        item.DelaiSlaHeures = model.DelaiSlaHeures;
        item.AssigneParDefautId = model.AssigneParDefautId;
        item.GroupeParDefautId = model.GroupeParDefautId;
        item.EstActif = model.EstActif;
        await db.SaveChangesAsync();
        TempData["Success"] = "Service mis a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var item = await db.SupportCatalogueServices.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.EstActif = !item.EstActif;
        await db.SaveChangesAsync();
        TempData["Success"] = item.EstActif ? "Service reactive." : "Service desactive.";
        return RedirectToAction(nameof(Index));
    }

    private bool CanManage() =>
        User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport");

    private async Task PopulateSupportTargetsAsync()
    {
        var supportRoleNames = new[] { "Administrateur", "Gestionnaire", "AgentSupport" };
        var roleAgentIds = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => supportRoleNames.Contains(x.Name!))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        ViewBag.SupportAgents = await db.Users
            .Where(u => roleAgentIds.Contains(u.Id) && u.IsActive)
            .OrderBy(u => u.Prenom)
            .ThenBy(u => u.Nom)
            .ToListAsync();

        ViewBag.GroupesSupport = await db.Groupes
            .Where(g => g.IsActive)
            .OrderBy(g => g.Nom)
            .ToListAsync();
    }
}
