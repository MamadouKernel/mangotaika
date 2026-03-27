using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class GroupesController(IGroupeService groupeService, IFileUploadService fileUploadService) : Controller
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
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupeCreateDto dto, IFormFile? Logo)
    {
        if (!ModelState.IsValid) return View(dto);
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
            return View(dto);
        }
        TempData["Success"] = "Groupe créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var groupe = await groupeService.GetByIdAsync(id);
        if (groupe is null) return NotFound();
        return View(groupe);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var groupe = await groupeService.GetByIdAsync(id);
        if (groupe is null) return NotFound();
        return View(groupe);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, GroupeCreateDto dto, IFormFile? Logo)
    {
        if (!ModelState.IsValid) return View(ToEditDto(id, dto));
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
            return View(ToEditDto(id, dto));
        }
        if (!result) return NotFound();
        TempData["Success"] = "Groupe mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await groupeService.DeleteAsync(id);
        TempData["Success"] = "Groupe désactivé.";
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
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }


}
