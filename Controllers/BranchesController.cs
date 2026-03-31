using MangoTaika.Data;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class BranchesController(IBrancheService brancheService, AppDbContext db, IFileUploadService fileUploadService) : Controller
{
    public async Task<IActionResult> Index(Guid? entiteId, Guid? groupeId, string? nomBranche)
    {
        var (page, ps) = ListPagination.Read(Request);
        var allBranches = await brancheService.GetAllAsync();
        var filteredBranches = allBranches.AsEnumerable();
        var selectedEntiteId = entiteId.HasValue && entiteId.Value != Guid.Empty
            ? entiteId
            : (groupeId.HasValue && groupeId.Value != Guid.Empty ? groupeId : null);

        if (selectedEntiteId.HasValue)
        {
            filteredBranches = filteredBranches.Where(b => b.GroupeId == selectedEntiteId.Value);
        }

        var normalizedNomBranche = string.IsNullOrWhiteSpace(nomBranche)
            ? null
            : DatabaseText.NormalizeSearchKey(nomBranche);

        if (!string.IsNullOrEmpty(normalizedNomBranche))
        {
            filteredBranches = filteredBranches.Where(b => DatabaseText.NormalizeSearchKey(b.Nom) == normalizedNomBranche);
        }

        var filteredList = filteredBranches
            .OrderBy(b => b.NomGroupe)
            .ThenBy(b => b.Nom)
            .ToList();

        var total = filteredList.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = filteredList.Skip(skip).Take(pageSize).ToList();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);

        var entites = await db.Groupes
            .Where(g => g.IsActive)
            .OrderBy(g => g.Nom)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Nom
            })
            .ToListAsync();

        var nomsBranches = allBranches
            .GroupBy(b => DatabaseText.NormalizeSearchKey(b.Nom))
            .Select(group => group
                .OrderBy(b => b.Nom, StringComparer.CurrentCultureIgnoreCase)
                .First())
            .OrderBy(b => b.Nom, StringComparer.CurrentCultureIgnoreCase)
            .Select(b => new SelectListItem
            {
                Value = b.Nom,
                Text = b.Nom
            })
            .ToList();

        var vm = new BranchesIndexViewModel
        {
            Branches = pageItems,
            EntiteId = selectedEntiteId,
            NomBranche = string.IsNullOrWhiteSpace(nomBranche) ? null : nomBranche.Trim(),
            Entites = entites,
            NomsBranches = nomsBranches
        };

        return View(vm);
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
    public async Task<IActionResult> Create(BrancheCreateDto dto, IFormFile? Logo)
    {
        if (ModelState.IsValid)
        {
            await ValidateBrancheAsync(dto);
        }

        if (!ModelState.IsValid)
        {
            await LoadGroupesAsync();
            return View(dto);
        }

        try
        {
            dto.LogoUrl = await fileUploadService.SaveImageAsync(
                Logo,
                dto.LogoUrl,
                "branches",
                "Le logo de la branche doit etre une image valide de 5 Mo maximum.");
            await brancheService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadGroupesAsync();
            this.AddDomainError(ex);
            return View(dto);
        }

        TempData["Success"] = "Branche enregistree avec succes.";
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
    public async Task<IActionResult> Edit(Guid id, BrancheCreateDto dto, IFormFile? Logo)
    {
        if (ModelState.IsValid)
        {
            await ValidateBrancheAsync(dto, id);
        }

        if (!ModelState.IsValid)
        {
            await LoadGroupesAsync();
            return View(ToEditDto(id, dto));
        }

        bool result;
        try
        {
            dto.LogoUrl = await fileUploadService.SaveImageAsync(
                Logo,
                dto.LogoUrl,
                "branches",
                "Le logo de la branche doit etre une image valide de 5 Mo maximum.");
            result = await brancheService.UpdateAsync(id, dto);
        }
        catch (InvalidOperationException ex)
        {
            await LoadGroupesAsync();
            this.AddDomainError(ex);
            return View(ToEditDto(id, dto));
        }

        if (!result) return NotFound();
        TempData["Success"] = "Branche mise a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await brancheService.DeleteAsync(id);
        TempData["Success"] = "Branche desactivee.";
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
            .Include(s => s.Branche)
            .Where(s => s.GroupeId == groupeId
                && s.IsActive
                && s.BrancheId.HasValue
                && s.Branche != null
                && s.Branche.GroupeId == groupeId)
            .OrderBy(s => s.Prenom)
            .ThenBy(s => s.Nom)
            .ToListAsync();

        var items = scouts
            .Where(s => IsChefUniteFunction(s.Fonction))
            .Select(s => new
            {
                s.Id,
                Nom = s.Prenom + " " + s.Nom + " (" + s.Matricule + ")"
                    + (s.Branche != null ? " - " + s.Branche.Nom : string.Empty)
                    + " - " + GetChefUniteRoleLabel(s.Fonction)
            })
            .ToList();

        return Json(items);
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
            ModelState.AddModelError(nameof(dto.GroupeId), "Le groupe selectionne est introuvable ou inactif.");
            return;
        }

        var nom = dto.Nom.Trim();
        var normalizedNom = DatabaseText.NormalizeSearchKey(nom);
        var duplicateExists = db.Database.IsNpgsql()
            ? await db.Branches.AnyAsync(b =>
                b.IsActive &&
                b.GroupeId == dto.GroupeId &&
                b.Id != currentBrancheId &&
                b.NomNormalise == normalizedNom)
            : (await db.Branches
                .Where(b => b.IsActive && b.GroupeId == dto.GroupeId && b.Id != currentBrancheId)
                .Select(b => b.Nom)
                .ToListAsync())
                .Any(existingNom => DatabaseText.NormalizeSearchKey(existingNom) == normalizedNom);

        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(dto.Nom), "Une branche avec ce nom existe deja dans ce groupe.");
        }

        if (!dto.ChefUniteId.HasValue)
        {
            return;
        }


        var chef = await db.Scouts
            .Where(s => s.Id == dto.ChefUniteId.Value && s.IsActive)
            .Select(s => new
            {
                s.GroupeId,
                s.Fonction,
                s.BrancheId,
                BrancheGroupeId = s.Branche != null ? (Guid?)s.Branche.GroupeId : null
            })
            .FirstOrDefaultAsync();

        if (chef is null)
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le responsable de branche selectionne est introuvable.");
            return;
        }

        if (chef.GroupeId != dto.GroupeId || !chef.BrancheId.HasValue || chef.BrancheGroupeId != dto.GroupeId)
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le responsable de branche doit appartenir au groupe selectionne et etre rattache a une branche de ce groupe.");
        }

        if (!IsChefUniteFunction(chef.Fonction))
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le responsable de branche doit avoir la fonction CHEF D'UNITE (CU) ou CHEF D'UNITE ADJOINT (CUA).");
        }
    }

    private static bool IsChefUniteFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);
        return normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF D'UNITE (CU)")
            || normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF D'UNITE ADJOINT (CUA)");
    }

    private static string GetChefUniteRoleLabel(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);
        return normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF D'UNITE ADJOINT (CUA)")
            ? "CUA"
            : "CU";
    }

    private static BrancheDto ToEditDto(Guid id, BrancheCreateDto dto) => new()
    {
        Id = id,
        Nom = dto.Nom,
        Description = dto.Description,
        LogoUrl = dto.LogoUrl,
        AgeMin = dto.AgeMin,
        AgeMax = dto.AgeMax,
        NomChefUnite = null,
        ChefUniteId = dto.ChefUniteId,
        GroupeId = dto.GroupeId
    };
}





