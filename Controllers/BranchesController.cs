using MangoTaika.Data;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant,EquipeDistrict,ChefGroupe,ChefUnite")]
public class BranchesController(
    IBrancheService brancheService,
    AppDbContext db,
    IFileUploadService fileUploadService,
    OperationalAccessService operationalAccess,
    ActiveRoleService activeRoleService) : Controller
{
    public async Task<IActionResult> Index(Guid? entiteId, Guid? groupeId, string? nomBranche)
    {
        var (page, ps) = ListPagination.Read(Request);
        var allBranches = await brancheService.GetAllAsync();
        var filteredBranches = allBranches.AsEnumerable();

        var activeRole = activeRoleService.GetActiveRole(User);
        if (activeRole is "EquipeDistrict" or "ChefGroupe" or "ChefUnite")
        {
            var (scopeGroupeId, scopeBrancheId) = await operationalAccess.GetScopeAsync(User, activeRole);
            if (scopeBrancheId.HasValue)
                filteredBranches = filteredBranches.Where(b => b.Id == scopeBrancheId.Value);
            else if (scopeGroupeId.HasValue)
                filteredBranches = filteredBranches.Where(b => b.GroupeId == scopeGroupeId.Value);
        }

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
            .ThenBy(b => BranchOrdering.GetSortWeight(b.Nom))
            .ThenBy(b => b.AgeMin ?? int.MaxValue)
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
                .OrderBy(b => BranchOrdering.GetSortWeight(b.Nom))
                .ThenBy(b => b.AgeMin ?? int.MaxValue)
                .ThenBy(b => b.Nom, StringComparer.CurrentCultureIgnoreCase)
                .First())
            .OrderBy(b => BranchOrdering.GetSortWeight(b.Nom))
            .ThenBy(b => b.AgeMin ?? int.MaxValue)
            .ThenBy(b => b.Nom, StringComparer.CurrentCultureIgnoreCase)
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
        return View(new BrancheCreateDto());
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
        var groupeNom = await db.Groupes
            .Where(g => g.Id == groupeId && g.IsActive)
            .Select(g => g.Nom)
            .FirstOrDefaultAsync();

        var isDistrictEquipe = IsDistrictEquipe(groupeNom);

        var scouts = await db.Scouts
            .Include(s => s.Branche)
            .Where(s => s.GroupeId == groupeId
                && s.IsActive)
            .OrderBy(s => s.Prenom)
            .ThenBy(s => s.Nom)
            .ToListAsync();

        var items = scouts
            .Where(s => IsEligibleResponsableFunction(s.Fonction, isDistrictEquipe))
            .Select(s => new
            {
                s.Id,
                Nom = s.Prenom + " " + s.Nom + " (" + s.Matricule + ")"
                    + (s.Branche != null ? " - " + s.Branche.Nom : string.Empty)
                    + " - " + GetResponsableRoleLabel(s.Fonction)
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

        var groupeNom = await db.Groupes
            .Where(g => g.Id == dto.GroupeId && g.IsActive)
            .Select(g => g.Nom)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(groupeNom))
        {
            ModelState.AddModelError(nameof(dto.GroupeId), "Le groupe selectionne est introuvable ou inactif.");
            return;
        }

        var isDistrictEquipe = IsDistrictEquipe(groupeNom);

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
                s.Fonction
            })
            .FirstOrDefaultAsync();

        if (chef is null)
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le responsable de branche selectionne est introuvable.");
            return;
        }

        if (chef.GroupeId != dto.GroupeId)
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), "Le responsable de branche doit appartenir au groupe selectionne.");
        }

        if (!IsEligibleResponsableFunction(chef.Fonction, isDistrictEquipe))
        {
            ModelState.AddModelError(nameof(dto.ChefUniteId), GetResponsableSelectionMessage(isDistrictEquipe));
        }
    }

    private static bool IsDistrictEquipe(string? groupeNom)
    {
        return DatabaseText.NormalizeSearchKey(groupeNom ?? string.Empty)
            == DatabaseText.NormalizeSearchKey("Equipe de District Mango Taika");
    }

    private static bool IsEligibleResponsableFunction(string? fonction, bool isDistrictEquipe)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);

        if (isDistrictEquipe)
        {
            return normalizedFunction == DatabaseText.NormalizeSearchKey("ASSISTANT COMMISSAIRE DE DISTRICT (ACD)");
        }

        return normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF D'UNITE (CU)")
            || normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF D'UNITE ADJOINT (CUA)");
    }

    private static string GetResponsableRoleLabel(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);

        if (normalizedFunction == DatabaseText.NormalizeSearchKey("ASSISTANT COMMISSAIRE DE DISTRICT (ACD)"))
        {
            return "ACD";
        }

        return normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF D'UNITE ADJOINT (CUA)")
            ? "CUA"
            : "CU";
    }

    private static string GetResponsableSelectionMessage(bool isDistrictEquipe)
    {
        return isDistrictEquipe
            ? "Pour les branches du groupe Equipe de District Mango Taika, le responsable de branche doit avoir la fonction ASSISTANT COMMISSAIRE DE DISTRICT (ACD)."
            : "Le responsable de branche doit avoir la fonction CHEF D'UNITE (CU) ou CHEF D'UNITE ADJOINT (CUA).";
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
        ContactChefUnite = null,
        ChefUniteId = dto.ChefUniteId,
        GroupeId = dto.GroupeId
    };
}
