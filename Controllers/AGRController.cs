using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class AGRController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid UserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var query = db.ProjetsAGR
            .Include(p => p.Groupe).Include(p => p.Transactions.Where(t => !t.EstSupprime))
            .Where(p => !p.EstSupprime)
            .OrderByDescending(p => p.DateCreation);
        var all = await query.ToListAsync();
        var total = all.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var projets = all.Skip(skip).Take(pageSize).ToList();
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(projets);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjetAGR model)
    {
        model.Id = Guid.NewGuid();
        model.CreateurId = UserId;
        if (model.GroupeId == Guid.Empty) model.GroupeId = null;
        db.ProjetsAGR.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Projet AGR créé.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var projet = await db.ProjetsAGR
            .Include(p => p.Groupe)
            .Include(p => p.Transactions.Where(t => !t.EstSupprime).OrderByDescending(t => t.DateTransaction))
            .FirstOrDefaultAsync(p => p.Id == id && !p.EstSupprime);
        if (projet is null) return NotFound();
        return View(projet);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var projet = await db.ProjetsAGR.Include(p => p.Groupe).FirstOrDefaultAsync(p => p.Id == id && !p.EstSupprime);
        if (projet is null) return NotFound();
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();
        return View(projet);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProjetAGR model)
    {
        var p = await db.ProjetsAGR.FindAsync(id);
        if (p is null || p.EstSupprime) return NotFound();
        p.Nom = model.Nom;
        p.Description = model.Description;
        p.BudgetInitial = model.BudgetInitial;
        p.DateDebut = model.DateDebut;
        p.DateFin = model.DateFin;
        p.Responsable = model.Responsable;
        p.Statut = model.Statut;
        p.GroupeId = model.GroupeId == Guid.Empty ? null : model.GroupeId;
        await db.SaveChangesAsync();
        TempData["Success"] = "Projet mis à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterTransaction(Guid id, TransactionFinanciere model)
    {
        model.Id = Guid.NewGuid();
        model.ProjetAGRId = id;
        model.Categorie = CategorieFinance.AGR;
        model.CreateurId = UserId;
        db.TransactionsFinancieres.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Transaction ajoutée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangerStatut(Guid id, StatutProjetAGR statut)
    {
        var p = await db.ProjetsAGR.FindAsync(id);
        if (p is not null) { p.Statut = statut; await db.SaveChangesAsync(); }
        TempData["Success"] = "Statut mis à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await db.ProjetsAGR.FindAsync(id);
        if (p is not null) { p.EstSupprime = true; await db.SaveChangesAsync(); }
        TempData["Success"] = "Projet supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
