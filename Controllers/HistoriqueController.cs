using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

public class HistoriqueController(AppDbContext db) : Controller
{
    [AllowAnonymous]
    public Task<IActionResult> Commissaires(string? recherche)
        => ListeParCategorieAsync(
            CategorieHistorique.AncienCommissaire,
            "Anciens Commissaires de District",
            "bi-award-fill",
            recherche);

    [AllowAnonymous]
    public Task<IActionResult> ChefsGroupe(string? recherche)
        => ListeParCategorieAsync(
            CategorieHistorique.AncienChefGroupe,
            "Anciens Chefs de Groupe",
            "bi-people-fill",
            recherche);

    [AllowAnonymous]
    public Task<IActionResult> CAD(string? recherche)
        => ListeParCategorieAsync(
            CategorieHistorique.MembreCAD,
            "Membres du CAD",
            "bi-shield-fill-check",
            recherche);

    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
    public async Task<IActionResult> Index(string? recherche, List<CategorieHistorique>? categories)
    {
        var selectedCategories = NormalizeCategories(categories);
        var query = BuildHistoriqueQuery(recherche);

        if (selectedCategories.Count > 0)
        {
            var selectedFlags = CombineCategories(selectedCategories);
            query = query.Where(m => (m.Categories & selectedFlags) != CategorieHistorique.Aucune);
        }

        var ordered = query
            .OrderBy(m => m.Ordre)
            .ThenBy(m => m.Nom);

        var (page, ps) = ListPagination.Read(Request);
        var total = await ordered.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var membres = await ordered.Skip(skip).Take(pageSize).ToListAsync();

        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(new HistoriqueIndexViewModel
        {
            Recherche = recherche,
            Categories = selectedCategories,
            Membres = membres
        });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
    public async Task<IActionResult> Details(Guid id)
    {
        var membre = await db.MembresHistoriques
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);

        return membre is null ? NotFound() : View(membre);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Create()
        => View(new HistoriqueFormViewModel { Ordre = 0 });

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HistoriqueFormViewModel model, IFormFile? Photo)
    {
        var categories = model.ToFlags();
        if (categories == CategorieHistorique.Aucune)
        {
            ModelState.AddModelError(nameof(model.Categories), "Sélectionnez au moins une catégorie.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var membre = new MembreHistorique
        {
            Id = Guid.NewGuid(),
            Nom = model.Nom.Trim(),
            Description = NormalizeValue(model.Description),
            Periode = NormalizeValue(model.Periode),
            Categories = categories,
            Ordre = model.Ordre
        };

        if (Photo is not null && Photo.Length > 0)
        {
            membre.PhotoUrl = await SavePhotoAsync(Photo);
        }

        db.MembresHistoriques.Add(membre);
        await db.SaveChangesAsync();

        TempData["Success"] = "Membre ajouté.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var membre = await db.MembresHistoriques.FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        return membre is null ? NotFound() : View(HistoriqueFormViewModel.FromEntity(membre));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HistoriqueFormViewModel model, IFormFile? Photo)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var categories = model.ToFlags();
        if (categories == CategorieHistorique.Aucune)
        {
            ModelState.AddModelError(nameof(model.Categories), "Sélectionnez au moins une catégorie.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var membre = await db.MembresHistoriques.FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        if (membre is null)
        {
            return NotFound();
        }

        membre.Nom = model.Nom.Trim();
        membre.Description = NormalizeValue(model.Description);
        membre.Periode = NormalizeValue(model.Periode);
        membre.Categories = categories;
        membre.Ordre = model.Ordre;

        if (Photo is not null && Photo.Length > 0)
        {
            membre.PhotoUrl = await SavePhotoAsync(Photo);
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
        var membre = await db.MembresHistoriques.FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        if (membre is not null)
        {
            membre.EstSupprime = true;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Membre supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ListeParCategorieAsync(
        CategorieHistorique category,
        string title,
        string icon,
        string? recherche)
    {
        var membres = await BuildHistoriqueQuery(recherche)
            .Where(m => (m.Categories & category) == category)
            .OrderBy(m => m.Ordre)
            .ThenBy(m => m.Nom)
            .ToListAsync();

        ViewBag.Titre = title;
        ViewBag.Icone = icon;
        ViewBag.Categorie = category;
        ViewBag.Recherche = recherche;
        return View("Liste", membres);
    }

    private IQueryable<MembreHistorique> BuildHistoriqueQuery(string? recherche)
    {
        var query = db.MembresHistoriques
            .AsNoTracking()
            .Where(m => !m.EstSupprime);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var term = recherche.Trim();
            var pattern = $"%{term}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.Nom, pattern) ||
                (m.Description != null && EF.Functions.ILike(m.Description, pattern)) ||
                (m.Periode != null && EF.Functions.ILike(m.Periode, pattern)));
        }

        return query;
    }

    private static List<CategorieHistorique> NormalizeCategories(List<CategorieHistorique>? categories)
        => categories?
            .Where(category => category != CategorieHistorique.Aucune)
            .Where(category => CategorieHistoriqueExtensions.All.Contains(category))
            .Distinct()
            .ToList()
           ?? [];

    private static CategorieHistorique CombineCategories(IEnumerable<CategorieHistorique> categories)
        => categories.Aggregate(CategorieHistorique.Aucune, static (current, category) => current | category);

    private static string? NormalizeValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<string> SavePhotoAsync(IFormFile photo)
    {
        var directory = Path.Combine("wwwroot", "uploads", "historique");
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
        var filePath = Path.Combine(directory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await photo.CopyToAsync(stream);

        return $"/uploads/historique/{fileName}";
    }
}
