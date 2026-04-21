using MangoTaika.Data;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant,AssistantCommissaire,ChefGroupe,ChefUnite")]
public class ScoutsController(
    IScoutService scoutService,
    IScoutQrService scoutQrService,
    AppDbContext db,
    IMemoryCache memoryCache,
    IFileUploadService fileUploadService,
    OperationalAccessService operationalAccess,
    ActiveRoleService activeRoleService) : Controller
{
    private const string ImportReportCachePrefix = "scouts-import-report:";
    private static readonly TimeSpan ImportReportLifetime = TimeSpan.FromMinutes(15);

    private async Task LoadDropdownsAsync()
    {
        ViewBag.Groupes = await db.Groupes
            .Where(g => g.IsActive)
            .OrderBy(g => g.Nom)
            .ToListAsync();

        ViewBag.Branches = await db.Branches
            .Where(b => b.IsActive)
            .ToListAsync();
    }

    public async Task<IActionResult> Index(string? recherche, Guid? groupeId, Guid? brancheId, bool cu = false, bool acd = false, string? importReportId = null)
    {
        var (page, ps) = ListPagination.Read(Request);
        var scouts = string.IsNullOrWhiteSpace(recherche)
            ? await scoutService.GetAllAsync()
            : await scoutService.SearchAsync(recherche);

        var filtered = scouts.AsEnumerable();

        var activeRole = activeRoleService.GetActiveRole(User);
        if (activeRole is "AssistantCommissaire" or "ChefGroupe" or "ChefUnite")
        {
            var (scopeGroupeId, scopeBrancheId) = await operationalAccess.GetScopeAsync(User, activeRole);
            if (scopeBrancheId.HasValue)
                filtered = filtered.Where(s => s.BrancheId == scopeBrancheId.Value);
            else if (scopeGroupeId.HasValue)
                filtered = filtered.Where(s => s.GroupeId == scopeGroupeId.Value);
        }

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            filtered = filtered.Where(s => s.GroupeId == groupeId.Value);
        }

        if (brancheId.HasValue && brancheId.Value != Guid.Empty)
        {
            filtered = filtered.Where(s => s.BrancheId == brancheId.Value);
        }

        if (cu || acd)
        {
            filtered = filtered.Where(s =>
                (cu && HasScoutFunction(s.Fonction, "CHEF D'UNITE (CU)")) ||
                (acd && HasScoutFunction(s.Fonction, "ASSISTANT COMMISSAIRE DE DISTRICT (ACD)")));
        }

        var filteredList = filtered
            .OrderBy(s => s.Prenom)
            .ThenBy(s => s.Nom)
            .ToList();

        var total = filteredList.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = filteredList.Skip(skip).Take(pageSize).ToList();

        await LoadDropdownsAsync();
        ViewBag.Recherche = recherche;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedBrancheId = brancheId;
        ViewBag.FilterCu = cu;
        ViewBag.FilterAcd = acd;
        ViewBag.ImportError = TempData["ImportError"] as string;
        ViewBag.ImportReport = ResolveImportReport(importReportId);
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new ScoutCreateDto());
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScoutCreateDto dto, IFormFile? Photo)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(dto);
        }

        ScoutDto scout;
        try
        {
            dto.PhotoUrl = await fileUploadService.SaveImageAsync(
                Photo,
                dto.PhotoUrl,
                "scouts",
                "La photo du scout doit etre une image valide de 5 Mo maximum.");
            scout = await scoutService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadDropdownsAsync();
            this.AddDomainError(ex);
            return View(dto);
        }

        TempData["Success"] = "Scout cree avec succes. Le matricule sera attribue lors de la premiere cotisation nationale.";
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
    public async Task<IActionResult> Edit(Guid id, ScoutCreateDto dto, IFormFile? Photo)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(dto);
        }

        bool result;
        try
        {
            dto.PhotoUrl = await fileUploadService.SaveImageAsync(
                Photo,
                dto.PhotoUrl,
                "scouts",
                "La photo du scout doit etre une image valide de 5 Mo maximum.");
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

        var importReportId = StoreImportReport(result);
        return RedirectToAction(nameof(Index), new { importReportId });
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

        ViewBag.ScoutQrValue = scoutQrService.GenerateScoutCode(id);

        ViewBag.InscriptionsAnnuelles = await db.InscriptionsAnnuellesScouts.AsNoTracking()
            .Include(i => i.Groupe)
            .Include(i => i.Branche)
            .Where(i => i.ScoutId == id)
            .OrderByDescending(i => i.AnneeReference)
            .ToListAsync();

        return View(scout);
    }

    [HttpGet]
    public async Task<IActionResult> GetBranchesByGroupe(Guid groupeId)
    {
        var branches = (await db.Branches
                .Where(b => b.GroupeId == groupeId && b.IsActive)
                .ToListAsync())
            .OrderBy(b => BranchOrdering.GetSortWeight(b.Nom))
            .ThenBy(b => b.AgeMin ?? int.MaxValue)
            .ThenBy(b => b.Nom)
            .Select(b => new { b.Id, b.Nom, b.AgeMin, b.AgeMax })
            .ToList();

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

    private static bool HasScoutFunction(string? fonction, string expectedFunction)
    {
        return DatabaseText.NormalizeSearchKey(fonction ?? string.Empty)
            == DatabaseText.NormalizeSearchKey(expectedFunction);
    }

    private static ScoutCreateDto ToCreateDto(ScoutDto scout) => new()
    {
        Matricule = scout.Matricule,
        Nom = scout.Nom,
        Prenom = scout.Prenom,
        DateNaissance = scout.DateNaissance,
        LieuNaissance = scout.LieuNaissance,
        Sexe = scout.Sexe,
        PhotoUrl = scout.PhotoUrl,
        Telephone = scout.Telephone,
        Email = scout.Email,
        RegionScoute = scout.RegionScoute,
        District = scout.District,
        NumeroCarte = scout.NumeroCarte,
        Fonction = scout.Fonction,
        FonctionVieActive = scout.FonctionVieActive,
        NiveauFormationScoute = scout.NiveauFormationScoute,
        ContactUrgenceNom = scout.ContactUrgenceNom,
        ContactUrgenceRelation = scout.ContactUrgenceRelation,
        ContactUrgenceTelephone = scout.ContactUrgenceTelephone,
        AssuranceAnnuelle = scout.AssuranceAnnuelle,
        AdresseGeographique = scout.AdresseGeographique,
        GroupeId = scout.GroupeId,
        BrancheId = scout.BrancheId
    };

    private string StoreImportReport(ScoutImportResultDto result)
    {
        var reportId = Guid.NewGuid().ToString("N");
        memoryCache.Set($"{ImportReportCachePrefix}{reportId}", result, ImportReportLifetime);
        return reportId;
    }

    private ScoutImportResultDto? ResolveImportReport(string? importReportId)
    {
        if (string.IsNullOrWhiteSpace(importReportId))
        {
            return null;
        }

        return memoryCache.TryGetValue($"{ImportReportCachePrefix}{importReportId}", out ScoutImportResultDto? report)
            ? report
            : null;
    }
}

