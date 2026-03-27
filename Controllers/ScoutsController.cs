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
    private const int MaxDisplayedImportErrors = 3;
    private const int MaxDisplayedImportErrorLength = 120;

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

        ScoutDto scout;
        try
        {
            scout = await scoutService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadDropdownsAsync();
            this.AddDomainError(ex);
            return View(dto);
        }
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
        return View(ToCreateDto(scout));
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

        bool result;
        try
        {
            result = await scoutService.UpdateAsync(id, dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadDropdownsAsync();
            this.AddDomainError(ex);
            return View(dto);
        }
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
        ScoutImportResultDto result;
        try
        {
            result = await scoutService.ImportFromExcelAsync(stream);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ImportError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ImportError"] = "Une erreur est survenue pendant l'import du fichier Excel.";
            return RedirectToAction(nameof(Index));
        }

        if (result.CreatedCount > 0 || result.UpdatedCount > 0)
        {
            TempData["Success"] =
                $"{result.CreatedCount} scout(s) cree(s) et {result.UpdatedCount} scout(s) mis a jour.";
        }

        if (result.CreatedCount == 0 && result.UpdatedCount == 0 && result.Errors.Count > 0)
        {
            TempData["ImportError"] = "Aucun scout n'a pu etre importe. Corrigez les lignes en erreur, verifiez le format du modele Excel, puis recommencez.";
        }

        if (result.Errors.Count > 0 || result.SkippedCount > 0)
        {
            TempData["ImportSummary"] =
                $"{result.CreatedCount} cree(s), {result.UpdatedCount} mis a jour, {result.SkippedCount} non enregistre(s).";
            var previewErrors = result.Errors
                .Take(MaxDisplayedImportErrors)
                .Select(error => new ScoutImportErrorDto
                {
                    LineNumber = error.LineNumber,
                    Message = TruncateImportErrorMessage(error.Message)
                })
                .ToList();

            TempData["ImportErrors"] = JsonSerializer.Serialize(previewErrors);

            var omittedCount = Math.Max(0, result.Errors.Count - previewErrors.Count);
            if (omittedCount > 0)
            {
                TempData["ImportErrorsNotice"] =
                    $"Affichage limite aux {previewErrors.Count} premieres erreurs. {omittedCount} autre(s) erreur(s) ne sont pas affichee(s).";
            }
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

    private static ScoutCreateDto ToCreateDto(ScoutDto scout) => new()
    {
        Matricule = scout.Matricule,
        Nom = scout.Nom,
        Prenom = scout.Prenom,
        DateNaissance = scout.DateNaissance,
        LieuNaissance = scout.LieuNaissance,
        Sexe = scout.Sexe,
        Telephone = scout.Telephone,
        Email = scout.Email,
        RegionScoute = scout.RegionScoute,
        District = scout.District,
        NumeroCarte = scout.NumeroCarte,
        Fonction = scout.Fonction,
        AssuranceAnnuelle = scout.AssuranceAnnuelle,
        AdresseGeographique = scout.AdresseGeographique,
        GroupeId = scout.GroupeId,
        BrancheId = scout.BrancheId
    };

    private static string TruncateImportErrorMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length <= MaxDisplayedImportErrorLength)
        {
            return message;
        }

        return $"{message[..(MaxDisplayedImportErrorLength - 3)].TrimEnd()}...";
    }
}
