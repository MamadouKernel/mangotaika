using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire")]
public class MotCommissaireController(AppDbContext db, IFileUploadService fileUploadService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var mots = await db.MotsCommissaire.Where(m => !m.EstSupprime).OrderByDescending(m => m.Annee).ToListAsync();
        return View(mots);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var mot = await db.MotsCommissaire.FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        if (mot is null) return NotFound();
        return View(mot);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MotCommissaire model, IFormFile? Photo)
    {
        ModelState.Remove("PhotoUrl");
        if (!ModelState.IsValid) return View(model);
        model.Id = Guid.NewGuid();
        if (Photo is not null)
        {
            try
            {
                model.PhotoUrl = await fileUploadService.SaveImageAsync(Photo, "commissaire");
            }
            catch (InvalidOperationException ex)
            {
                this.AddDomainError(ex);
                return View(model);
            }
        }
        db.MotsCommissaire.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Mot du commissaire ajouté.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var mot = await db.MotsCommissaire.FindAsync(id);
        if (mot is null) return NotFound();
        return View(mot);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MotCommissaire model, IFormFile? Photo)
    {
        ModelState.Remove("PhotoUrl");
        if (!ModelState.IsValid) return View(model);
        var mot = await db.MotsCommissaire.FindAsync(id);
        if (mot is null) return NotFound();
        mot.Contenu = model.Contenu;
        mot.Annee = model.Annee;
        mot.EstActif = model.EstActif;
        if (Photo is not null)
        {
            try
            {
                mot.PhotoUrl = await fileUploadService.SaveImageAsync(Photo, "commissaire");
            }
            catch (InvalidOperationException ex)
            {
                this.AddDomainError(ex);
                return View(model);
            }
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Mot du commissaire mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var mot = await db.MotsCommissaire.FindAsync(id);
        if (mot is not null)
        {
            mot.EstSupprime = true;
            await db.SaveChangesAsync();
            TempData["Success"] = "Mot du commissaire supprimé.";
        }
        return RedirectToAction(nameof(Index));
    }

}

