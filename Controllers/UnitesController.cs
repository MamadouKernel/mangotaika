using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,ChefUnite")]
public class UnitesController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IFileUploadService fileUpload,
    OperationalAccessService accessService) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(Guid? brancheId)
    {
        var scope = await GetScopeAsync();
        if (!scope.Allowed) return Forbid();

        var query = db.UnitesScoutes
            .Include(u => u.Groupe)
            .Include(u => u.Branche)
            .Include(u => u.Affectations)
            .AsNoTracking()
            .AsQueryable();

        if (scope.BrancheId.HasValue)
        {
            query = query.Where(u => u.BrancheId == scope.BrancheId.Value);
            brancheId = scope.BrancheId;
        }
        else if (scope.GroupeId.HasValue)
        {
            query = query.Where(u => u.GroupeId == scope.GroupeId.Value);
        }

        if (brancheId.HasValue && brancheId.Value != Guid.Empty)
        {
            query = query.Where(u => u.BrancheId == brancheId.Value);
        }

        await LoadBranchesAsync(scope, brancheId);
        return View(await query.OrderBy(u => u.Branche.Nom).ThenBy(u => u.Nom).ToListAsync());
    }

    public async Task<IActionResult> Create(Guid? brancheId)
    {
        var scope = await GetScopeAsync();
        if (!scope.Allowed || !scope.BrancheId.HasValue && User.IsInRole(RoleNames.ChefUnite)) return Forbid();
        var selectedBrancheId = scope.BrancheId ?? brancheId;
        if (selectedBrancheId.HasValue)
        {
            var selectedBranch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == selectedBrancheId.Value && b.IsActive);
            if (selectedBranch is null
                || scope.GroupeId.HasValue && selectedBranch.GroupeId != scope.GroupeId.Value
                || scope.BrancheId.HasValue && selectedBranch.Id != scope.BrancheId.Value)
            {
                return Forbid();
            }
        }

        await LoadBranchesAsync(scope, selectedBrancheId);
        if (selectedBrancheId.HasValue)
        {
            await LoadScoutsAsync(selectedBrancheId.Value);
        }
        else
        {
            ViewBag.Scouts = new List<Scout>();
        }
        return View(new UniteScoute { GroupeId = scope.GroupeId ?? Guid.Empty, BrancheId = selectedBrancheId ?? Guid.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UniteScoute model, IFormFile? Image, Guid[]? scoutIds, string? matricules)
    {
        var scope = await GetScopeAsync();
        if (!scope.Allowed) return Forbid();
        ApplyScope(model, scope);
        await ValidateUnitAsync(model, scope);

        if (!ModelState.IsValid)
        {
            await LoadBranchesAsync(scope, model.BrancheId);
            await LoadScoutsAsync(model.BrancheId);
            return View(model);
        }

        try
        {
            model.ImageUrl = Image is null ? Normalize(model.ImageUrl) : await fileUpload.SaveImageAsync(Image, "unites");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageUrl), ex.Message);
            await LoadBranchesAsync(scope, model.BrancheId);
            await LoadScoutsAsync(model.BrancheId);
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.Nom = model.Nom.Trim();
        model.Description = Normalize(model.Description);
        model.Attributs = Normalize(model.Attributs);
        model.CreateurId = CurrentUserId;
        db.UnitesScoutes.Add(model);
        await AddAffectationsAsync(model.Id, model.BrancheId, scoutIds, matricules);
        await db.SaveChangesAsync();

        TempData["Success"] = "Unite creee et affectations mises a jour.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var unite = await db.UnitesScoutes.Include(u => u.Affectations).FirstOrDefaultAsync(u => u.Id == id);
        if (unite is null) return NotFound();
        var scope = await GetScopeAsync();
        if (!CanManage(unite, scope)) return Forbid();

        await LoadBranchesAsync(scope, unite.BrancheId);
        await LoadScoutsAsync(unite.BrancheId);
        ViewBag.SelectedScoutIds = unite.Affectations.Select(a => a.ScoutId).ToHashSet();
        return View(unite);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UniteScoute model, IFormFile? Image, Guid[]? scoutIds, string? matricules)
    {
        var unite = await db.UnitesScoutes.Include(u => u.Affectations).FirstOrDefaultAsync(u => u.Id == id);
        if (unite is null) return NotFound();
        var scope = await GetScopeAsync();
        if (!CanManage(unite, scope)) return Forbid();

        model.Id = id;
        ApplyScope(model, scope, unite);
        await ValidateUnitAsync(model, scope, id);
        if (!ModelState.IsValid)
        {
            await LoadBranchesAsync(scope, model.BrancheId);
            await LoadScoutsAsync(model.BrancheId);
            ViewBag.SelectedScoutIds = unite.Affectations.Select(a => a.ScoutId).ToHashSet();
            return View(model);
        }

        try
        {
            unite.ImageUrl = Image is null ? Normalize(model.ImageUrl) : await fileUpload.SaveImageAsync(Image, "unites");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageUrl), ex.Message);
            await LoadBranchesAsync(scope, model.BrancheId);
            await LoadScoutsAsync(model.BrancheId);
            ViewBag.SelectedScoutIds = unite.Affectations.Select(a => a.ScoutId).ToHashSet();
            return View(model);
        }

        unite.Nom = model.Nom.Trim();
        unite.Description = Normalize(model.Description);
        unite.Attributs = Normalize(model.Attributs);
        unite.EstActive = model.EstActive;
        await ReplaceAffectationsAsync(unite, scoutIds, matricules);
        await db.SaveChangesAsync();

        TempData["Success"] = "Unite mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var unite = await db.UnitesScoutes
            .Include(u => u.Groupe)
            .Include(u => u.Branche)
            .Include(u => u.Roles.OrderBy(r => r.Nom))
            .Include(u => u.Affectations)
                .ThenInclude(a => a.Scout)
            .Include(u => u.Affectations)
                .ThenInclude(a => a.RoleUniteScoute)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (unite is null) return NotFound();
        var scope = await GetScopeAsync();
        if (!CanManage(unite, scope)) return Forbid();

        return View(unite);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var unite = await db.UnitesScoutes.FirstOrDefaultAsync(u => u.Id == id);
        if (unite is null) return NotFound();
        var scope = await GetScopeAsync();
        if (!CanManage(unite, scope)) return Forbid();

        unite.EstSupprime = true;
        unite.EstActive = false;
        await db.AffectationsUnitesScoutes.Where(a => a.UniteScouteId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.EstActif, false));
        await db.SaveChangesAsync();
        TempData["Success"] = "Unite retiree de la liste.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterRole(Guid uniteId, string nom, string? description)
    {
        var unite = await db.UnitesScoutes.FirstOrDefaultAsync(u => u.Id == uniteId);
        if (unite is null) return NotFound();
        var scope = await GetScopeAsync();
        if (!CanManage(unite, scope)) return Forbid();
        if (string.IsNullOrWhiteSpace(nom))
        {
            TempData["Error"] = "Le nom du role d'unite est obligatoire.";
            return RedirectToAction(nameof(Details), new { id = uniteId });
        }

        db.RolesUnitesScoutes.Add(new RoleUniteScoute
        {
            Id = Guid.NewGuid(),
            UniteScouteId = uniteId,
            Nom = nom.Trim(),
            Description = Normalize(description)
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Role d'unite ajoute.";
        return RedirectToAction(nameof(Details), new { id = uniteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AffecterRole(Guid affectationId, Guid? roleId)
    {
        var affectation = await db.AffectationsUnitesScoutes.Include(a => a.UniteScoute).FirstOrDefaultAsync(a => a.Id == affectationId);
        if (affectation is null) return NotFound();
        var scope = await GetScopeAsync();
        if (!CanManage(affectation.UniteScoute, scope)) return Forbid();
        affectation.RoleUniteScouteId = roleId == Guid.Empty ? null : roleId;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = affectation.UniteScouteId });
    }

    private async Task<(bool Allowed, Guid? GroupeId, Guid? BrancheId)> GetScopeAsync()
    {
        if (accessService.IsAdminLike(User))
        {
            return (true, null, null);
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        if (scout is null) return (false, null, null);
        if (User.IsInRole(RoleNames.ChefUnite))
        {
            return scout.GroupeId.HasValue && scout.BrancheId.HasValue
                ? (true, scout.GroupeId, scout.BrancheId)
                : (false, null, null);
        }

        if (User.IsInRole(RoleNames.ChefGroupe) && scout.GroupeId.HasValue)
        {
            return (true, scout.GroupeId, null);
        }

        return (false, null, null);
    }

    private static bool CanManage(UniteScoute unite, (bool Allowed, Guid? GroupeId, Guid? BrancheId) scope)
        => scope.Allowed
            && (!scope.GroupeId.HasValue || unite.GroupeId == scope.GroupeId.Value)
            && (!scope.BrancheId.HasValue || unite.BrancheId == scope.BrancheId.Value);

    private static void ApplyScope(UniteScoute model, (bool Allowed, Guid? GroupeId, Guid? BrancheId) scope, UniteScoute? existing = null)
    {
        if (scope.GroupeId.HasValue) model.GroupeId = scope.GroupeId.Value;
        if (scope.BrancheId.HasValue) model.BrancheId = scope.BrancheId.Value;
        if (existing is not null)
        {
            model.GroupeId = existing.GroupeId;
            model.BrancheId = existing.BrancheId;
        }
    }

    private async Task ValidateUnitAsync(UniteScoute model, (bool Allowed, Guid? GroupeId, Guid? BrancheId) scope, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(model.Nom))
        {
            ModelState.AddModelError(nameof(model.Nom), "Le nom de l'unite est obligatoire.");
        }

        var branche = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == model.BrancheId && b.IsActive);
        if (branche is null)
        {
            ModelState.AddModelError(nameof(model.BrancheId), "Selectionnez une branche active.");
            return;
        }

        if (model.GroupeId == Guid.Empty)
        {
            model.GroupeId = branche.GroupeId;
        }

        if (branche.GroupeId != model.GroupeId)
        {
            ModelState.AddModelError(nameof(model.BrancheId), "La branche selectionnee n'appartient pas au groupe de l'unite.");
        }

        if (scope.GroupeId.HasValue && model.GroupeId != scope.GroupeId.Value)
        {
            ModelState.AddModelError(nameof(model.GroupeId), "Vous ne pouvez gerer que les unites de votre groupe.");
        }

        if (scope.BrancheId.HasValue && model.BrancheId != scope.BrancheId.Value)
        {
            ModelState.AddModelError(nameof(model.BrancheId), "Un chef d'unite ne peut gerer que sa branche.");
        }

        if (!string.IsNullOrWhiteSpace(model.Nom)
            && await db.UnitesScoutes.AnyAsync(u => u.Id != currentId && u.BrancheId == model.BrancheId && u.Nom == model.Nom.Trim()))
        {
            ModelState.AddModelError(nameof(model.Nom), "Une unite porte deja ce nom dans cette branche.");
        }
    }

    private async Task LoadBranchesAsync((bool Allowed, Guid? GroupeId, Guid? BrancheId) scope, Guid? selectedBrancheId)
    {
        var query = db.Branches.Include(b => b.Groupe).Where(b => b.IsActive).AsQueryable();
        if (scope.BrancheId.HasValue) query = query.Where(b => b.Id == scope.BrancheId.Value);
        else if (scope.GroupeId.HasValue) query = query.Where(b => b.GroupeId == scope.GroupeId.Value);
        ViewBag.Branches = await query.OrderBy(b => b.Groupe.Nom).ThenBy(b => b.Nom).ToListAsync();
        ViewBag.SelectedBrancheId = selectedBrancheId;
    }

    private async Task LoadScoutsAsync(Guid brancheId)
    {
        ViewBag.Scouts = await db.Scouts
            .Where(s => s.IsActive && s.BrancheId == brancheId)
            .OrderBy(s => s.Nom)
            .ThenBy(s => s.Prenom)
            .ToListAsync();
    }

    private async Task AddAffectationsAsync(Guid uniteId, Guid brancheId, Guid[]? scoutIds, string? matricules)
    {
        var ids = await ResolveScoutIdsAsync(brancheId, scoutIds, matricules);
        foreach (var scoutId in ids)
        {
            await db.AffectationsUnitesScoutes.Where(a => a.ScoutId == scoutId && a.EstActif).ExecuteUpdateAsync(s => s.SetProperty(a => a.EstActif, false));
            db.AffectationsUnitesScoutes.Add(new AffectationUniteScoute { Id = Guid.NewGuid(), UniteScouteId = uniteId, ScoutId = scoutId });
        }
    }

    private async Task ReplaceAffectationsAsync(UniteScoute unite, Guid[]? scoutIds, string? matricules)
    {
        foreach (var existing in unite.Affectations)
        {
            existing.EstActif = false;
        }
        await AddAffectationsAsync(unite.Id, unite.BrancheId, scoutIds, matricules);
    }

    private async Task<List<Guid>> ResolveScoutIdsAsync(Guid brancheId, Guid[]? scoutIds, string? matricules)
    {
        var requestedIds = (scoutIds ?? []).Where(id => id != Guid.Empty).ToHashSet();
        var matriculeKeys = (matricules ?? string.Empty)
            .Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(DatabaseText.NormalizeSearchKey)
            .Where(v => v.Length > 0)
            .ToHashSet();

        var scouts = await db.Scouts
            .Where(s => s.IsActive && s.BrancheId == brancheId)
            .Select(s => new { s.Id, s.Matricule })
            .ToListAsync();

        return scouts
            .Where(s => requestedIds.Contains(s.Id) || (s.Matricule != null && matriculeKeys.Contains(DatabaseText.NormalizeSearchKey(s.Matricule))))
            .Select(s => s.Id)
            .Distinct()
            .ToList();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
