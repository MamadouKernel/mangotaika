using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MangoTaika.Controllers;

public class ActualitesController(IActualiteService actualiteService, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : Controller
{
    // ─── Public (sans login) ───
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var actualites = await actualiteService.GetAllPublishedAsync();
        return View(actualites);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id)
    {
        var a = await actualiteService.GetByIdAsync(id);
        if (a is null || !a.EstPublie) return NotFound();
        return View(a);
    }

    // ─── Admin CRUD ───
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Admin()
    {
        var (page, ps) = ListPagination.Read(Request);
        var all = await actualiteService.GetAllAsync();
        var total = all.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = all.OrderByDescending(a => a.DatePublication).Skip(skip).Take(pageSize).ToList();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(ActualiteCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        string? imagePath = await SaveImageAsync(dto.Image);
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        await actualiteService.CreateAsync(dto, userId, imagePath);
        TempData["Success"] = "Actualité créée avec succès.";
        return RedirectToAction(nameof(Admin));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var a = await actualiteService.GetByIdAsync(id);
        if (a is null) return NotFound();
        return View(a);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id, ActualiteCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        string? imagePath = await SaveImageAsync(dto.Image);
        var result = await actualiteService.UpdateAsync(id, dto, imagePath);
        if (!result) return NotFound();
        TempData["Success"] = "Actualité mise à jour.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Publier(Guid id)
    {
        await actualiteService.PublierAsync(id);
        TempData["Success"] = "Actualité publiée.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Depublier(Guid id)
    {
        await actualiteService.DepublierAsync(id);
        TempData["Success"] = "Actualité dépubliée.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await actualiteService.DeleteAsync(id);
        TempData["Success"] = "Actualité supprimée.";
        return RedirectToAction(nameof(Admin));
    }

    private async Task<string?> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        var uploads = Path.Combine(env.WebRootPath, "uploads", "actualites");
        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploads, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/actualites/{fileName}";
    }
}
