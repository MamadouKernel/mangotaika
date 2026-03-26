using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

public class HistoriqueController(AppDbContext db) : Controller
{
    // Pages publiques
    [AllowAnonymous]
    public async Task<IActionResult> Commissaires()
    {
        var membres = await db.MembresHistoriques
            .Where(m => m.Categorie == CategorieHistorique.AncienCommissaire && !m.EstSupprime)
            .OrderBy(m => m.Ordre).ToListAsync();
        ViewBag.Titre = "Anciens Commissaires de District";
        ViewBag.Icone = "bi-award-fill";
        ViewBag.Categorie = CategorieHistorique.AncienCommissaire;
        return View("Liste", membres);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ChefsGroupe()
    {
        var membres = await db.MembresHistoriques
            .Where(m => m.Categorie == CategorieHistorique.AncienChefGroupe && !m.EstSupprime)
            .OrderBy(m => m.Ordre).ToListAsync();
        ViewBag.Titre = "Anciens Chefs de Groupe";
        ViewBag.Icone = "bi-people-fill";
        ViewBag.Categorie = CategorieHistorique.AncienChefGroupe;
        return View("Liste", membres);
    }

    [AllowAnonymous]
    public async Task<IActionResult> CAD()
    {
        var membres = await db.MembresHistoriques
            .Where(m => m.Categorie == CategorieHistorique.MembreCAD && !m.EstSupprime)
            .OrderBy(m => m.Ordre).ToListAsync();
        ViewBag.Titre = "Membres du CAD";
        ViewBag.Icone = "bi-shield-fill-check";
        ViewBag.Categorie = CategorieHistorique.MembreCAD;
        return View("Liste", membres);
    }

    // Admin CRUD
    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var query = db.MembresHistoriques.Where(m => !m.EstSupprime).OrderBy(m => m.Categorie).ThenBy(m => m.Ordre);
        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var membres = await query.Skip(skip).Take(pageSize).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(membres);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
    public async Task<IActionResult> Details(Guid id)
    {
        var m = await db.MembresHistoriques.FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        if (m is null) return NotFound();
        return View(m);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Create() => View();

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MembreHistorique model, IFormFile? Photo)
    {
        ModelState.Remove("PhotoUrl");
        if (!ModelState.IsValid) return View(model);
        model.Id = Guid.NewGuid();
        if (Photo is not null && Photo.Length > 0)
        {
            var dir = Path.Combine("wwwroot", "uploads", "historique");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await Photo.CopyToAsync(stream);
            model.PhotoUrl = $"/uploads/historique/{fileName}";
        }
        db.MembresHistoriques.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Membre ajouté.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var m = await db.MembresHistoriques.FindAsync(id);
        if (m is null) return NotFound();
        return View(m);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MembreHistorique model, IFormFile? Photo)
    {
        ModelState.Remove("PhotoUrl");
        if (!ModelState.IsValid) return View(model);
        var m = await db.MembresHistoriques.FindAsync(id);
        if (m is null) return NotFound();
        m.Nom = model.Nom;
        m.Description = model.Description;
        m.Periode = model.Periode;
        m.Categorie = model.Categorie;
        m.Ordre = model.Ordre;
        if (Photo is not null && Photo.Length > 0)
        {
            var dir = Path.Combine("wwwroot", "uploads", "historique");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await Photo.CopyToAsync(stream);
            m.PhotoUrl = $"/uploads/historique/{fileName}";
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Membre mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var m = await db.MembresHistoriques.FindAsync(id);
        if (m is not null) { m.EstSupprime = true; await db.SaveChangesAsync(); }
        TempData["Success"] = "Membre supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
