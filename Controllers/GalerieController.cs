using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire")]
public class GalerieController(AppDbContext db, IFileUploadService fileUpload) : Controller
{
    private static readonly HashSet<string> ExtensionsVideo = [".mp4", ".webm", ".ogg", ".mov"];

    public async Task<IActionResult> Index()
    {
        var medias = await db.Galeries
            .Where(g => !g.EstSupprime)
            .OrderByDescending(g => g.DateUpload)
            .ToListAsync();
        return View(medias);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var item = await db.Galeries.FirstOrDefaultAsync(g => g.Id == id && !g.EstSupprime);
        if (item is null) return NotFound();
        return View(item);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await db.Galeries.FirstOrDefaultAsync(g => g.Id == id && !g.EstSupprime);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string titre, string? description, bool estPublie)
    {
        var item = await db.Galeries.FindAsync(id);
        if (item is null || item.EstSupprime) return NotFound();
        if (string.IsNullOrWhiteSpace(titre))
        {
            ModelState.AddModelError("titre", "Le titre est requis.");
            var again = await db.Galeries.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && !g.EstSupprime);
            return again is null ? NotFound() : View(again);
        }
        item.Titre = titre.Trim();
        item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        item.EstPublie = estPublie;
        await db.SaveChangesAsync();
        TempData["Success"] = "Média mis à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string titre, string? description, IFormFile[]? medias)
    {
        var fichiers = medias?.Where(f => f is { Length: > 0 }).ToList() ?? [];
        if (fichiers.Count == 0)
        {
            ModelState.AddModelError("medias", "Au moins un fichier média est requis.");
            return View();
        }
        if (string.IsNullOrWhiteSpace(titre))
        {
            ModelState.AddModelError("titre", "Le titre est requis.");
            return View();
        }

        var titreBase = titre.Trim();
        var errors = new List<string>();
        var ajoutes = 0;
        for (var i = 0; i < fichiers.Count; i++)
        {
            var media = fichiers[i];
            if (!fileUpload.IsValidMedia(media))
            {
                errors.Add($"Fichier non accepté : {media.FileName}");
                continue;
            }

            var ext = Path.GetExtension(media.FileName).ToLowerInvariant();
            var typeMedia = ExtensionsVideo.Contains(ext) ? "video" : "image";
            var path = await fileUpload.SaveFileAsync(media, "galerie");

            var titreGalerie = fichiers.Count > 1 ? $"{titreBase} ({i + 1})" : titreBase;
            db.Galeries.Add(new Galerie
            {
                Id = Guid.NewGuid(),
                Titre = titreGalerie,
                Description = description,
                CheminMedia = path,
                TypeMedia = typeMedia,
                EstPublie = true
            });
            ajoutes++;
        }

        if (ajoutes == 0)
        {
            foreach (var e in errors)
                ModelState.AddModelError("medias", e);
            if (!errors.Any())
                ModelState.AddModelError("medias", "Aucun fichier valide n'a pu être enregistré.");
            return View();
        }

        await db.SaveChangesAsync();
        TempData["Success"] = ajoutes == 1
            ? "Média ajouté à la galerie."
            : $"{ajoutes} médias ajoutés à la galerie.";
        if (errors.Count > 0)
            TempData["Warning"] = string.Join(" ", errors);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publier(Guid id)
    {
        var item = await db.Galeries.FindAsync(id);
        if (item is not null && !item.EstSupprime)
        {
            item.EstPublie = true;
            await db.SaveChangesAsync();
            TempData["Success"] = "Média publié.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Depublier(Guid id)
    {
        var item = await db.Galeries.FindAsync(id);
        if (item is not null && !item.EstSupprime)
        {
            item.EstPublie = false;
            await db.SaveChangesAsync();
            TempData["Success"] = "Média masqué.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await db.Galeries.FindAsync(id);
        if (item is not null)
        {
            item.EstSupprime = true;
            await db.SaveChangesAsync();
            TempData["Success"] = "Média supprimé.";
        }
        return RedirectToAction(nameof(Index));
    }
}
