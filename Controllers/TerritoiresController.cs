using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
public class TerritoiresController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var regions = await db.RegionsScoutes
            .Include(r => r.Districts.Where(d => d.EstActif))
            .ThenInclude(d => d.Groupes.Where(g => g.IsActive))
            .OrderBy(r => r.Nom)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.DistrictsSansRegion = await db.DistrictsScouts
            .Include(d => d.Groupes.Where(g => g.IsActive))
            .Where(d => d.EstActif && d.RegionScouteId == null)
            .OrderBy(d => d.Nom)
            .AsNoTracking()
            .ToListAsync();

        return View(regions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreerRegion(string nom, string? description)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            TempData["Error"] = "Le nom de la region est obligatoire.";
            return RedirectToAction(nameof(Index));
        }

        db.RegionsScoutes.Add(new RegionScoute
        {
            Id = Guid.NewGuid(),
            Nom = nom.Trim(),
            Description = NormalizeOptional(description)
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Region creee.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModifierRegion(Guid id, string nom, string? description, bool estActive)
    {
        var region = await db.RegionsScoutes.FindAsync(id);
        if (region is null) return NotFound();
        if (string.IsNullOrWhiteSpace(nom))
        {
            TempData["Error"] = "Le nom de la region est obligatoire.";
            return RedirectToAction(nameof(Index));
        }

        region.Nom = nom.Trim();
        region.Description = NormalizeOptional(description);
        region.EstActive = estActive;
        await db.SaveChangesAsync();
        TempData["Success"] = "Region mise a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreerDistrict(string nom, Guid? regionScouteId, string? description)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            TempData["Error"] = "Le nom du district est obligatoire.";
            return RedirectToAction(nameof(Index));
        }

        db.DistrictsScouts.Add(new DistrictScout
        {
            Id = Guid.NewGuid(),
            Nom = nom.Trim(),
            RegionScouteId = regionScouteId,
            Description = NormalizeOptional(description)
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "District cree.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModifierDistrict(Guid id, string nom, Guid? regionScouteId, string? description, bool estActif)
    {
        var district = await db.DistrictsScouts.FindAsync(id);
        if (district is null) return NotFound();
        if (string.IsNullOrWhiteSpace(nom))
        {
            TempData["Error"] = "Le nom du district est obligatoire.";
            return RedirectToAction(nameof(Index));
        }

        district.Nom = nom.Trim();
        district.RegionScouteId = regionScouteId;
        district.Description = NormalizeOptional(description);
        district.EstActif = estActif;
        await db.SaveChangesAsync();
        TempData["Success"] = "District mis a jour.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
