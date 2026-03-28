using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class FinancesController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid UserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(int? annee)
    {
        var year = annee ?? DateTime.Now.Year;
        var (page, ps) = ListPagination.Read(Request);

        var previousYearBalance = await db.TransactionsFinancieres.AsNoTracking()
            .Where(t => !t.EstSupprime && t.DateTransaction.Year == year - 1)
            .Select(t => t.Type == TypeTransaction.Recette ? t.Montant : -t.Montant)
            .SumAsync();

        var baseQuery = db.TransactionsFinancieres.AsNoTracking()
            .Where(t => !t.EstSupprime && t.DateTransaction.Year == year);

        var totalRecettes = await baseQuery.Where(t => t.Type == TypeTransaction.Recette).SumAsync(t => (decimal?)t.Montant) ?? 0m;
        var totalDepenses = await baseQuery.Where(t => t.Type == TypeTransaction.Depense).SumAsync(t => (decimal?)t.Montant) ?? 0m;

        var totalCount = await baseQuery.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, totalCount);

        var transactions = await baseQuery
            .OrderByDescending(t => t.DateTransaction)
            .Skip(skip)
            .Take(pageSize)
            .Include(t => t.Groupe)
            .Include(t => t.Scout)
            .ToListAsync();

        var parCatRows = await baseQuery
            .GroupBy(t => t.Categorie)
            .Select(g => new
            {
                Cat = g.Key,
                Recettes = g.Where(x => x.Type == TypeTransaction.Recette).Sum(x => (decimal?)x.Montant) ?? 0m,
                Depenses = g.Where(x => x.Type == TypeTransaction.Depense).Sum(x => (decimal?)x.Montant) ?? 0m
            })
            .ToListAsync();
        ViewBag.ParCategorie = parCatRows
            .Select(x => new { Categorie = x.Cat.ToString(), x.Recettes, x.Depenses })
            .OrderByDescending(x => x.Recettes + x.Depenses)
            .ToList();

        ViewBag.Annee = year;
        ViewBag.ReportANouveau = previousYearBalance;
        ViewBag.TotalRecettes = totalRecettes;
        ViewBag.TotalDepenses = totalDepenses;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Activites = await db.Activites.Where(a => !a.EstSupprime).OrderByDescending(a => a.DateCreation).Take(20).ToListAsync();
        ViewBag.ProjetsAGR = await db.ProjetsAGR.Where(p => !p.EstSupprime).OrderBy(p => p.Nom).ToListAsync();
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();

        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, totalCount, totalPages);
        return View(transactions);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var t = await db.TransactionsFinancieres.AsNoTracking()
            .Include(x => x.Groupe).Include(x => x.Scout).Include(x => x.Activite).Include(x => x.ProjetAGR)
            .Include(x => x.Createur)
            .FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        if (t is null) return NotFound();
        return View(t);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var t = await db.TransactionsFinancieres
            .FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        if (t is null) return NotFound();
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();
        ViewBag.Activites = await db.Activites.Where(a => !a.EstSupprime).OrderByDescending(a => a.DateCreation).Take(50).ToListAsync();
        ViewBag.ProjetsAGR = await db.ProjetsAGR.Where(p => !p.EstSupprime).OrderBy(p => p.Nom).ToListAsync();
        return View(t);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TransactionFinanciere model)
    {
        var t = await db.TransactionsFinancieres.FindAsync(id);
        if (t is null || t.EstSupprime) return NotFound();

        t.Libelle = model.Libelle;
        t.Montant = model.Montant;
        t.Type = model.Type;
        t.Categorie = model.Categorie;
        t.DateTransaction = model.DateTransaction;
        t.Reference = model.Reference;
        t.Commentaire = model.Commentaire;
        t.GroupeId = model.GroupeId == Guid.Empty ? null : model.GroupeId;
        t.ScoutId = model.ScoutId == Guid.Empty ? null : model.ScoutId;
        t.ActiviteId = model.ActiviteId == Guid.Empty ? null : model.ActiviteId;
        t.ProjetAGRId = model.ProjetAGRId == Guid.Empty ? null : model.ProjetAGRId;

        await db.SaveChangesAsync();
        TempData["Success"] = "Transaction mise à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionFinanciere model)
    {
        model.Id = Guid.NewGuid();
        model.CreateurId = UserId;
        if (model.GroupeId == Guid.Empty) model.GroupeId = null;
        if (model.ActiviteId == Guid.Empty) model.ActiviteId = null;
        if (model.ProjetAGRId == Guid.Empty) model.ProjetAGRId = null;
        if (model.ScoutId == Guid.Empty) model.ScoutId = null;
        db.TransactionsFinancieres.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Transaction enregistrée.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var t = await db.TransactionsFinancieres.FindAsync(id);
        if (t is not null) { t.EstSupprime = true; await db.SaveChangesAsync(); }
        TempData["Success"] = "Transaction supprimée.";
        return RedirectToAction(nameof(Index));
    }
}
