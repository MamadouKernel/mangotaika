using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Models;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

public class HistoriqueController(AppDbContext db, IFileUploadService fileUploadService) : Controller
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
        IQueryable<MembreHistorique> query = BuildHistoriqueQuery(recherche)
            .Include(m => m.CategorieDetails);

        if (selectedCategories.Count > 0)
        {
            query = query.Where(m => m.CategorieDetails.Any(detail => selectedCategories.Contains(detail.Categorie)));
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
            .Include(m => m.CategorieDetails.OrderBy(detail => detail.Ordre).ThenBy(detail => detail.Categorie))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);

        return membre is null ? NotFound() : View(membre);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Create()
        => View(new HistoriqueFormViewModel());

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HistoriqueFormViewModel model)
    {
        var entries = model.GetNormalizedEntries();
        if (entries.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Entries), "Sélectionnez au moins une catégorie.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var membre = new MembreHistorique
        {
            Id = Guid.NewGuid(),
            Nom = model.Nom.Trim(),
            CategorieDetails = entries.Select(entry => new MembreHistoriqueCategorie
            {
                Id = Guid.NewGuid(),
                Categorie = entry.Categorie,
                PhotoUrl = NormalizeValue(entry.PhotoUrl),
                Description = entry.Description,
                Periode = entry.Periode,
                Ordre = entry.Ordre
            }).ToList()
        };

        if (!await TryApplyEntryPhotosAsync(entries, membre.CategorieDetails.ToList()))
        {
            return View(model);
        }

        SyncLegacyFields(membre);
        db.MembresHistoriques.Add(membre);
        await db.SaveChangesAsync();

        TempData["Success"] = "Membre ajouté.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var membre = await db.MembresHistoriques
            .Include(m => m.CategorieDetails.OrderBy(detail => detail.Ordre).ThenBy(detail => detail.Categorie))
            .FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        return membre is null ? NotFound() : View(HistoriqueFormViewModel.FromEntity(membre));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HistoriqueFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var entries = model.GetNormalizedEntries();
        if (entries.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Entries), "Sélectionnez au moins une catégorie.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var membre = await db.MembresHistoriques
            .Include(m => m.CategorieDetails)
            .FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        if (membre is null)
        {
            return NotFound();
        }

        membre.Nom = model.Nom.Trim();
        db.MembresHistoriquesCategories.RemoveRange(membre.CategorieDetails.ToList());
        membre.CategorieDetails = [];

        foreach (var entry in entries)
        {
            membre.CategorieDetails.Add(new MembreHistoriqueCategorie
            {
                Id = Guid.NewGuid(),
                MembreHistoriqueId = membre.Id,
                Categorie = entry.Categorie,
                PhotoUrl = NormalizeValue(entry.PhotoUrl),
                Description = entry.Description,
                Periode = entry.Periode,
                Ordre = entry.Ordre
            });
        }

        if (!await TryApplyEntryPhotosAsync(entries, membre.CategorieDetails.ToList()))
        {
            return View(model);
        }

        SyncLegacyFields(membre);
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
            .Include(m => m.CategorieDetails)
            .Where(m => m.CategorieDetails.Any(detail => detail.Categorie == category))
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
                (m.Periode != null && EF.Functions.ILike(m.Periode, pattern)) ||
                m.CategorieDetails.Any(detail =>
                    (detail.Description != null && EF.Functions.ILike(detail.Description, pattern)) ||
                    (detail.Periode != null && EF.Functions.ILike(detail.Periode, pattern))));
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

    private static void SyncLegacyFields(MembreHistorique membre)
    {
        var details = membre.CategorieDetails
            .Where(detail => CategorieHistoriqueExtensions.All.Contains(detail.Categorie))
            .OrderBy(detail => detail.Ordre)
            .ThenBy(detail => detail.Categorie)
            .ToList();

        membre.Categories = details
            .Select(detail => detail.Categorie)
            .Aggregate(CategorieHistorique.Aucune, static (current, category) => current | category);
        membre.Ordre = details.FirstOrDefault()?.Ordre ?? 0;
        membre.Periode = details.Count == 1 ? NormalizeValue(details[0].Periode) : null;
        membre.Description = details.Count == 1 ? NormalizeValue(details[0].Description) : null;
        membre.PhotoUrl = details
            .Select(detail => NormalizeValue(detail.PhotoUrl))
            .FirstOrDefault(photoUrl => !string.IsNullOrWhiteSpace(photoUrl));
    }

    private static string? NormalizeValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<bool> TryApplyEntryPhotosAsync(
        IReadOnlyList<HistoriqueCategorieFormViewModel> entries,
        IReadOnlyList<MembreHistoriqueCategorie> details)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            var uploadedPhoto = Request.Form.Files.GetFile($"EntryPhotos[{index}]");
            if (uploadedPhoto is null || uploadedPhoto.Length == 0)
            {
                details[index].PhotoUrl = NormalizeValue(entries[index].PhotoUrl);
                continue;
            }

            try
            {
                details[index].PhotoUrl = await fileUploadService.SaveImageAsync(uploadedPhoto, "historique");
            }
            catch (InvalidOperationException ex)
            {
                this.AddDomainError(ex);
                return false;
            }
        }

        return true;
    }
}
