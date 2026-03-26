using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class GroupesController(IGroupeService groupeService) : Controller
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
    public async Task<IActionResult> Create(GroupeCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await groupeService.CreateAsync(dto);
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
    public async Task<IActionResult> Edit(Guid id, GroupeCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await groupeService.UpdateAsync(id, dto);
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
                cg = g.NomResponsable ?? "",
                adjoints = g.NomAdjoints ?? "",
                branches = g.BranchesScouts.Select(b => new { nom = b.Nom, scouts = b.NombreScouts, cu = b.NomChefUnite ?? "" })
            }));
        return View(groupes);
    }


}
