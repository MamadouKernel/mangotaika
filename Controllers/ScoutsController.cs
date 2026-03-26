using MangoTaika.Data;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class ScoutsController(IScoutService scoutService, AppDbContext db) : Controller
{
    private async Task LoadDropdownsAsync()
    {
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Branches = await db.Branches.Where(b => b.IsActive).OrderBy(b => b.Nom).ToListAsync();
    }

    public async Task<IActionResult> Index(string? recherche)
    {
        var (page, ps) = ListPagination.Read(Request);
        var scouts = string.IsNullOrWhiteSpace(recherche)
            ? await scoutService.GetAllAsync()
            : await scoutService.SearchAsync(recherche);
        var total = scouts.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = scouts.Skip(skip).Take(pageSize).ToList();
        ViewBag.Recherche = recherche;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScoutCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(dto);
        }

        var scout = await scoutService.CreateAsync(dto);
        TempData["Success"] = $"Scout cree avec succes. Matricule attribue : {scout.Matricule}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var scout = await scoutService.GetByIdAsync(id);
        if (scout is null)
        {
            return NotFound();
        }

        await LoadDropdownsAsync();
        return View(scout);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ScoutCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(dto);
        }

        var result = await scoutService.UpdateAsync(id, dto);
        if (!result)
        {
            return NotFound();
        }

        TempData["Success"] = "Scout mis a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await scoutService.DeleteAsync(id);
        TempData["Success"] = "Scout desactive.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile? fichier)
    {
        if (fichier is null || fichier.Length == 0)
        {
            TempData["ImportError"] = "Veuillez selectionner un fichier Excel.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(fichier.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ImportError"] = "Le fichier doit etre au format .xlsx.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = fichier.OpenReadStream();
        var result = await scoutService.ImportFromExcelAsync(stream);

        if (result.CreatedCount > 0)
        {
            TempData["Success"] = $"{result.CreatedCount} scout(s) importe(s) avec succes.";
        }

        if (result.CreatedCount == 0 && result.Errors.Count > 0)
        {
            TempData["ImportError"] = "Aucun scout n'a ete importe. Corrigez le fichier puis recommencez.";
        }

        if (result.Errors.Count > 0 || result.SkippedCount > 0)
        {
            TempData["ImportSummary"] = $"{result.CreatedCount} cree(s), {result.SkippedCount} ignore(s), {result.Errors.Count} erreur(s).";
            TempData["ImportErrors"] = JsonSerializer.Serialize(result.Errors.Take(15).ToList());
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult DownloadImportTemplate()
    {
        var content = scoutService.GenerateImportTemplate();
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"modele-import-scouts-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var scout = await scoutService.GetByIdAsync(id);
        if (scout is null)
        {
            return NotFound();
        }

        return View(scout);
    }

    [HttpGet]
    public async Task<IActionResult> GetBranchesByGroupe(Guid groupeId)
    {
        var branches = await db.Branches
            .Where(b => b.GroupeId == groupeId && b.IsActive)
            .OrderBy(b => b.AgeMin)
            .Select(b => new { b.Id, b.Nom, b.AgeMin, b.AgeMax })
            .ToListAsync();
        return Json(branches);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifierASCCI(Guid id)
    {
        var scout = await db.Scouts.FindAsync(id);
        if (scout is null)
        {
            return NotFound();
        }

        scout.StatutASCCI = "Verifie le " + DateTime.Now.ToString("dd/MM/yyyy");
        await db.SaveChangesAsync();
        TempData["Success"] = $"Statut ASCCI verifie pour {scout.Prenom} {scout.Nom}.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
