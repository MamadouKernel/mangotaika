using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire")]
public class PartenairesController(AppDbContext db, IFileUploadService fileUpload) : Controller
{
    public async Task<IActionResult> Index()
    {
        var partenaires = await db.Partenaires.Where(p => !p.EstSupprime).OrderBy(p => p.Ordre).ToListAsync();
        var liens = await db.LiensReseauxSociaux.OrderBy(l => l.Ordre).ToListAsync();
        ViewBag.Liens = liens;
        return View(partenaires);
    }

    public async Task<IActionResult> DetailsPartenaire(Guid id)
    {
        var p = await db.Partenaires.FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        if (p is null) return NotFound();
        return View(p);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> EditPartenaire(Guid id)
    {
        var p = await db.Partenaires.FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        if (p is null) return NotFound();
        return View(p);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPartenaire(Guid id, Partenaire model, IFormFile? Logo)
    {
        var p = await db.Partenaires.FindAsync(id);
        if (p is null || p.EstSupprime) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Nom))
        {
            ModelState.AddModelError(nameof(model.Nom), "Le nom est requis.");
            model.Id = id;
            model.LogoUrl = p.LogoUrl;
            return View(model);
        }
        p.Nom = model.Nom.Trim();
        p.Description = model.Description;
        var siteWeb = SafeLinkHelper.NormalizeAllowedLink(model.SiteWeb);
        if (!string.IsNullOrWhiteSpace(model.SiteWeb) && siteWeb is null)
        {
            ModelState.AddModelError(nameof(model.SiteWeb), "Le site web doit utiliser http ou https.");
            model.Id = id;
            model.LogoUrl = p.LogoUrl;
            return View(model);
        }
        p.SiteWeb = siteWeb;
        p.TypePartenariat = model.TypePartenariat;
        p.Ordre = model.Ordre;
        p.EstActif = model.EstActif;
        if (Logo is not null && Logo.Length > 0)
        {
            if (!fileUpload.IsValidImage(Logo))
            {
                TempData["Error"] = "Type de fichier non autorisé pour le logo.";
                model.LogoUrl = p.LogoUrl;
                return View(model);
            }
            p.LogoUrl = await fileUpload.SaveImageAsync(Logo, "partenaires");
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Partenaire mis à jour.";
        return RedirectToAction(nameof(DetailsPartenaire), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePartenaire(Partenaire model, IFormFile? Logo)
    {
        var siteWeb = SafeLinkHelper.NormalizeAllowedLink(model.SiteWeb);
        if (!string.IsNullOrWhiteSpace(model.SiteWeb) && siteWeb is null)
        {
            TempData["Error"] = "Le site web doit utiliser http ou https.";
            return RedirectToAction(nameof(Index));
        }

        model.Id = Guid.NewGuid();
        model.SiteWeb = siteWeb;
        if (Logo is not null && Logo.Length > 0)
        {
            if (!fileUpload.IsValidImage(Logo))
            {
                TempData["Error"] = "Type de fichier non autorisé pour le logo.";
                return RedirectToAction(nameof(Index));
            }

            model.LogoUrl = await fileUpload.SaveImageAsync(Logo, "partenaires");
        }

        db.Partenaires.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Partenaire ajouté.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePartenaire(Guid id)
    {
        var p = await db.Partenaires.FindAsync(id);
        if (p is not null) { p.EstSupprime = true; await db.SaveChangesAsync(); }
        TempData["Success"] = "Partenaire supprimé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLien(LienReseauSocial model)
    {
        var normalizedUrl = SafeLinkHelper.NormalizeAllowedLink(model.Url, allowLocal: true);
        if (normalizedUrl is null)
        {
            TempData["Error"] = "L'URL du reseau social doit utiliser http, https ou un chemin local valide.";
            return RedirectToAction(nameof(Index));
        }

        model.Id = Guid.NewGuid();
        model.Url = normalizedUrl;
        db.LiensReseauxSociaux.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Lien ajouté.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLien(Guid id)
    {
        var l = await db.LiensReseauxSociaux.FindAsync(id);
        if (l is not null) { db.LiensReseauxSociaux.Remove(l); await db.SaveChangesAsync(); }
        TempData["Success"] = "Lien supprimé.";
        return RedirectToAction(nameof(Index));
    }
}

