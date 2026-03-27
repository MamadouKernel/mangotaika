using MangoTaika.Data;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class BranchesController(IBrancheService brancheService, AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var all = await brancheService.GetAllAsync();
        var total = all.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = all.Skip(skip).Take(pageSize).ToList();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadGroupesAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrancheCreateDto dto)
    {
        if (ModelState.IsValid)
        {
            await ValidateBrancheAsync(dto);
        }

        if (!ModelState.IsValid) { await LoadGroupesAsync(); return View(dto); }
        try
        {
            await brancheService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadGroupesAsync();
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
        TempData["Success"] = "Branche créée avec succès.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var branche = await brancheService.GetByIdAsync(id);
        if (branche is null) return NotFound();
        return View(branche);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var branche = await brancheService.GetByIdAsync(id);
        if (branche is null) return NotFound();
        await LoadGroupesAsync();
        return View(branche);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BrancheCreateDto dto)
    {
        if (ModelState.IsValid)
        {
            await ValidateBrancheAsync(dto, id);
        }

        if (!ModelState.IsValid) { await LoadGroupesAsync(); return View(ToEditDto(id, dto)); }

        bool result;
        try
        {
            result = await brancheService.UpdateAsync(id, dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadGroupesAsync();
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(ToEditDto(id, dto));
        }

        if (!result) return NotFound();
        TempData["Success"] = "Branche mise à jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await brancheService.DeleteAsync(id);
        TempData["Success"] = "Branche désactivée.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadGroupesAsync()
    {
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> GetScoutsByGroupe(Guid groupeId)
    {
        var scouts = await db.Scouts
            .Where(s => s.GroupeId == groupeId && s.IsActive)
            .OrderBy(s => s.Prenom)
            .ThenBy(s => s.Nom)
            .Select(s => new { s.Id, Nom = s.Prenom + " " + s.Nom + " (" + s.Matricule + ")" })
            .ToListAsync();
        return Json(scouts);
    }

    private async Task ValidateBrancheAsync(BrancheCreateDto dto, Guid? currentBrancheId = null)
    {
        if (dto.GroupeId == Guid.Empty)
        {
            return;
        }

        var groupeExists = await db.Groupes.AnyAsync(g => g.Id == dto.GroupeId && g.IsActive);
        if (!groupeExists)
        {
            ModelState.AddModelError(nameof(dto.GroupeId), "Le groupe sélectionné est introuvable ou inactif.");
            return;
        }

        var nom = dto.Nom.Trim();
        var duplicateExists = await db.Branches.AnyAsync(b =>
            b.IsActive &&
            b.GroupeId == dto.GroupeId &&
            b.Id != currentBrancheId &&
            b.Nom.ToUpper() == nom.ToUpper());

        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(dto.Nom), "Une branche avec ce nom existe déjà dans ce groupe.");
        }

        if (!dto.ChefUniteId.HasValue)
        {
            return;
        }

        var chef = await db.Scouts
            .Where(s => s.Id == dto.ChefUniteId.Value && s.IsActive)
            .Select(s => new { s.GroupeId })
            .FirstOrDefaultAsync();

        if (chef is null)
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le chef d'unité sélectionné est introuvable.");
            return;
        }

        if (chef.GroupeId != dto.GroupeId)
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le chef d'unité doit appartenir au groupe sélectionné.");
        }
    }

    private static BrancheDto ToEditDto(Guid id, BrancheCreateDto dto) => new()
    {
        Id = id,
        Nom = dto.Nom,
        Description = dto.Description,
        AgeMin = dto.AgeMin,
        AgeMax = dto.AgeMax,
        NomChefUnite = null,
        ChefUniteId = dto.ChefUniteId,
        GroupeId = dto.GroupeId
    };
}
