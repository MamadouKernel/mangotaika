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
public class GroupesController(IGroupeService groupeService, IFileUploadService fileUploadService, AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var all = await groupeService.GetAllAsync();
        var total = all.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = all.Skip(skip).Take(pageSize).ToList();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadChefsGroupeAsync(null);
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupeCreateDto dto, IFormFile? Logo)
    {
        if (!ModelState.IsValid)
        {
            await LoadChefsGroupeAsync(null, dto.ChefGroupeScoutId);
            return View(dto);
        }

        try
        {
            dto.LogoUrl = await fileUploadService.SaveImageAsync(
                Logo,
                dto.LogoUrl,
                "groupes",
                "Le logo du groupe doit etre une image valide de 5 Mo maximum.");
            await groupeService.CreateAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            this.AddDomainError(ex);
            await LoadChefsGroupeAsync(null, dto.ChefGroupeScoutId);
            return View(dto);
        }

        TempData["Success"] = "Groupe cree avec succes.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var groupe = await groupeService.GetByIdAsync(id);
        if (groupe is null)
        {
            return NotFound();
        }

        return View(groupe);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var groupe = await groupeService.GetByIdAsync(id);
        if (groupe is null)
        {
            return NotFound();
        }

        await LoadChefsGroupeAsync(id, groupe.ChefGroupeScoutId);
        return View(groupe);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, GroupeCreateDto dto, IFormFile? Logo)
    {
        if (!ModelState.IsValid)
        {
            await LoadChefsGroupeAsync(id, dto.ChefGroupeScoutId);
            return View(ToEditDto(id, dto));
        }

        bool result;
        try
        {
            dto.LogoUrl = await fileUploadService.SaveImageAsync(
                Logo,
                dto.LogoUrl,
                "groupes",
                "Le logo du groupe doit etre une image valide de 5 Mo maximum.");
            result = await groupeService.UpdateAsync(id, dto);
        }
        catch (InvalidOperationException ex)
        {
            this.AddDomainError(ex);
            await LoadChefsGroupeAsync(id, dto.ChefGroupeScoutId);
            return View(ToEditDto(id, dto));
        }

        if (!result)
        {
            return NotFound();
        }

        TempData["Success"] = "Groupe mis a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await groupeService.DeleteAsync(id);
        TempData["Success"] = "Groupe desactive.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Carte()
    {
        var groupes = await groupeService.GetAllAsync();
        ViewBag.GroupesJson = System.Text.Json.JsonSerializer.Serialize(
            groupes.Where(g => g.Latitude.HasValue && g.Longitude.HasValue).Select(g => new
            {
                nom = g.Nom,
                adresse = g.Adresse ?? "",
                lat = g.Latitude,
                lng = g.Longitude,
                membres = g.NombreMembres,
                chefGroupe = g.NomChefGroupe ?? "",
                branches = g.BranchesScouts.Select(b => new { nom = b.Nom, scouts = b.NombreScouts, cu = b.NomChefUnite ?? "" })
            }));
        return View(groupes);
    }

    private static GroupeDto ToEditDto(Guid id, GroupeCreateDto dto)
    {
        var parts = new[] { dto.Quartier, dto.Commune }.Where(p => !string.IsNullOrWhiteSpace(p));
        var adresse = string.Join(", ", parts);

        return new GroupeDto
        {
            Id = id,
            Nom = dto.Nom,
            Description = dto.Description,
            LogoUrl = dto.LogoUrl,
            Adresse = string.IsNullOrWhiteSpace(adresse) ? null : adresse,
            NomChefGroupe = dto.NomChefGroupe,
            ChefGroupeScoutId = dto.ChefGroupeScoutId,
            ResponsableId = dto.ResponsableId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }

    private static bool IsDistrictEquipe(string? groupeNom)
    {
        return DatabaseText.NormalizeSearchKey(groupeNom ?? string.Empty)
            == DatabaseText.NormalizeSearchKey("Equipe de District Mango Taika");
    }

    private static bool IsEligibleChefGroupeFunction(string? fonction, bool isDistrictEquipe)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);

        if (isDistrictEquipe)
        {
            return normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT (CD)")
                || normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT ADJOINT (CDA)")
                || normalizedFunction == DatabaseText.NormalizeSearchKey("ASSISTANT COMMISSAIRE DE DISTRICT (ACD)");
        }

        return normalizedFunction == DatabaseText.NormalizeSearchKey("CHEF DE GROUPE (CG)");
    }

    private static string? GetChefGroupeRoleLabel(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);

        if (normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT (CD)"))
        {
            return "CD";
        }

        if (normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT ADJOINT (CDA)"))
        {
            return "CDA";
        }

        if (normalizedFunction == DatabaseText.NormalizeSearchKey("ASSISTANT COMMISSAIRE DE DISTRICT (ACD)"))
        {
            return "ACD";
        }

        return null;
    }

    private async Task LoadChefsGroupeAsync(Guid? groupeId, Guid? selectedScoutId = null)
    {
        var items = new List<SelectListItem>();

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            var groupeNom = await db.Groupes
                .Where(g => g.Id == groupeId.Value && g.IsActive)
                .Select(g => g.Nom)
                .FirstOrDefaultAsync();

            var isDistrictEquipe = IsDistrictEquipe(groupeNom);

            var scouts = await db.Scouts
                .Include(s => s.Branche)
                .Where(s => s.IsActive && s.GroupeId == groupeId.Value)
                .OrderBy(s => s.Prenom)
                .ThenBy(s => s.Nom)
                .ToListAsync();

            items = scouts
                .Where(s => IsEligibleChefGroupeFunction(s.Fonction, isDistrictEquipe)
                    && (isDistrictEquipe
                        || (s.BrancheId.HasValue
                            && s.Branche != null
                            && s.Branche.GroupeId == groupeId.Value)))
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Prenom} {s.Nom}"
                        + (string.IsNullOrWhiteSpace(s.Matricule) ? string.Empty : $" ({s.Matricule})")
                        + (s.Branche == null ? string.Empty : $" - {s.Branche.Nom}")
                        + (GetChefGroupeRoleLabel(s.Fonction) is { Length: > 0 } roleLabel ? $" - {roleLabel}" : string.Empty),
                    Selected = selectedScoutId.HasValue && s.Id == selectedScoutId.Value
                })
                .ToList();
        }

        ViewBag.ChefsGroupe = items;
    }
}


