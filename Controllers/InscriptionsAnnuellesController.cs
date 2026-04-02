using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class InscriptionsAnnuellesController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid? CurrentUserId => Guid.TryParse(userManager.GetUserId(User), out var id) ? id : null;

    public async Task<IActionResult> Index(int? annee, Guid? groupeId, Guid? brancheId, StatutInscriptionAnnuelle? statut)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var (page, ps) = ListPagination.Read(Request);

        var query = db.InscriptionsAnnuellesScouts.AsNoTracking()
            .Include(i => i.Scout)
            .Include(i => i.Groupe)
            .Include(i => i.Branche)
            .AsQueryable();

        query = query.Where(i => i.AnneeReference == year);

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            query = query.Where(i => i.GroupeId == groupeId.Value);
        }

        if (brancheId.HasValue && brancheId.Value != Guid.Empty)
        {
            query = query.Where(i => i.BrancheId == brancheId.Value);
        }

        if (statut.HasValue)
        {
            query = query.Where(i => i.Statut == statut.Value);
        }

        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var inscriptions = await query
            .OrderBy(i => i.Scout.Nom)
            .ThenBy(i => i.Scout.Prenom)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Annee = year;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedBrancheId = brancheId;
        ViewBag.SelectedStatut = statut;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Branches = await db.Branches.Where(b => b.IsActive).OrderBy(b => b.Nom).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(inscriptions);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(Guid? scoutId)
    {
        var model = new InscriptionAnnuelleScout
        {
            AnneeReference = DateTime.UtcNow.Year,
            LibelleAnnee = BuildYearLabel(DateTime.UtcNow.Year),
            DateInscription = DateTime.UtcNow.Date,
            ScoutId = scoutId ?? Guid.Empty
        };

        if (scoutId.HasValue && scoutId.Value != Guid.Empty)
        {
            var scout = await db.Scouts.FirstOrDefaultAsync(s => s.Id == scoutId.Value && s.IsActive);
            if (scout is not null)
            {
                ApplySnapshotFromScout(model, scout, overwrite: true);
                model.CotisationNationaleAjour = model.AnneeReference == DateTime.UtcNow.Year && scout.AssuranceAnnuelle;
            }
        }

        await LoadReferenceDataAsync(model.ScoutId, model.GroupeId, model.BrancheId);
        return View("Upsert", model);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InscriptionAnnuelleScout model)
    {
        var scout = await ValidateModelAsync(model);
        if (scout is not null)
        {
            model.CotisationNationaleAjour = model.AnneeReference == DateTime.UtcNow.Year && scout.AssuranceAnnuelle;
        }

        if (!ModelState.IsValid || scout is null)
        {
            await LoadReferenceDataAsync(model.ScoutId, model.GroupeId, model.BrancheId);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.LibelleAnnee = NormalizeYearLabel(model.AnneeReference, model.LibelleAnnee);
        model.DateInscription = EnsureUtc(model.DateInscription == default ? DateTime.UtcNow : model.DateInscription);
        ApplySnapshotFromScout(model, scout, overwrite: false);

        if (model.Statut == StatutInscriptionAnnuelle.Validee)
        {
            model.DateValidation ??= DateTime.UtcNow;
            model.ValideParId ??= CurrentUserId;
        }

        db.InscriptionsAnnuellesScouts.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Inscription annuelle enregistree.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.AsNoTracking()
            .Include(i => i.Scout)
            .Include(i => i.Groupe)
            .Include(i => i.Branche)
            .Include(i => i.ValidePar)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();
        return View(inscription);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();
        await LoadReferenceDataAsync(inscription.ScoutId, inscription.GroupeId, inscription.BrancheId);
        return View("Upsert", inscription);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, InscriptionAnnuelleScout model)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();

        var scout = await ValidateModelAsync(model, id);
        model.CotisationNationaleAjour = inscription.CotisationNationaleAjour;
        if (!ModelState.IsValid || scout is null)
        {
            model.Id = id;
            await LoadReferenceDataAsync(model.ScoutId, model.GroupeId, model.BrancheId);
            return View("Upsert", model);
        }

        inscription.ScoutId = model.ScoutId;
        inscription.AnneeReference = model.AnneeReference;
        inscription.LibelleAnnee = NormalizeYearLabel(model.AnneeReference, model.LibelleAnnee);
        inscription.DateInscription = EnsureUtc(model.DateInscription == default ? inscription.DateInscription : model.DateInscription);
        inscription.Statut = model.Statut;
        inscription.InscriptionParoissialeValidee = model.InscriptionParoissialeValidee;
        inscription.Observations = model.Observations?.Trim();
        inscription.GroupeId = model.GroupeId;
        inscription.BrancheId = model.BrancheId;
        inscription.FonctionSnapshot = string.IsNullOrWhiteSpace(model.FonctionSnapshot) ? null : model.FonctionSnapshot.Trim();
        ApplySnapshotFromScout(inscription, scout, overwrite: false);

        if (inscription.Statut == StatutInscriptionAnnuelle.Validee)
        {
            inscription.DateValidation = model.DateValidation ?? inscription.DateValidation ?? DateTime.UtcNow;
            inscription.ValideParId = model.ValideParId ?? inscription.ValideParId ?? CurrentUserId;
        }
        else if (inscription.Statut == StatutInscriptionAnnuelle.Suspendue)
        {
            inscription.DateValidation = null;
            inscription.ValideParId = null;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Inscription annuelle mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();

        inscription.Statut = StatutInscriptionAnnuelle.Validee;
        inscription.DateValidation = DateTime.UtcNow;
        inscription.ValideParId = CurrentUserId;
        await db.SaveChangesAsync();

        TempData["Success"] = "Inscription annuelle validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<Scout?> ValidateModelAsync(InscriptionAnnuelleScout model, Guid? currentId = null)
    {
        var scout = await db.Scouts.FirstOrDefaultAsync(s => s.Id == model.ScoutId && s.IsActive);
        if (scout is null)
        {
            ModelState.AddModelError(nameof(model.ScoutId), "Le scout selectionne est introuvable ou inactif.");
            return null;
        }

        if (await db.InscriptionsAnnuellesScouts.AnyAsync(i => i.Id != currentId && i.ScoutId == model.ScoutId && i.AnneeReference == model.AnneeReference))
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "Une inscription annuelle existe deja pour ce scout sur cette annee.");
        }

        if (model.AnneeReference < 2000 || model.AnneeReference > 2100)
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "L'annee de reference est invalide.");
        }

        if (model.GroupeId.HasValue)
        {
            var groupeExiste = await db.Groupes.AnyAsync(g => g.Id == model.GroupeId.Value && g.IsActive);
            if (!groupeExiste)
            {
                ModelState.AddModelError(nameof(model.GroupeId), "Le groupe selectionne est introuvable ou inactif.");
            }
        }

        if (model.BrancheId.HasValue)
        {
            var branche = await db.Branches.Where(b => b.Id == model.BrancheId.Value && b.IsActive)
                .Select(b => new { b.GroupeId })
                .FirstOrDefaultAsync();
            if (branche is null)
            {
                ModelState.AddModelError(nameof(model.BrancheId), "La branche selectionnee est introuvable ou inactive.");
            }
            else if (model.GroupeId.HasValue && branche.GroupeId != model.GroupeId.Value)
            {
                ModelState.AddModelError(nameof(model.BrancheId), "La branche selectionnee doit appartenir au groupe selectionne.");
            }
        }

        return scout;
    }

    private async Task LoadReferenceDataAsync(Guid? selectedScoutId, Guid? selectedGroupeId, Guid? selectedBrancheId)
    {
        ViewBag.Scouts = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Nom)
            .ThenBy(s => s.Prenom)
            .ToListAsync();
        ViewBag.Groupes = await db.Groupes
            .Where(g => g.IsActive)
            .OrderBy(g => g.Nom)
            .ToListAsync();
        ViewBag.Branches = await db.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();
        ViewBag.SelectedScoutId = selectedScoutId;
        ViewBag.SelectedGroupeId = selectedGroupeId;
        ViewBag.SelectedBrancheId = selectedBrancheId;
    }

    private static void ApplySnapshotFromScout(InscriptionAnnuelleScout inscription, Scout scout, bool overwrite)
    {
        if (overwrite || !inscription.GroupeId.HasValue)
        {
            inscription.GroupeId = scout.GroupeId;
        }

        if (overwrite || !inscription.BrancheId.HasValue)
        {
            inscription.BrancheId = scout.BrancheId;
        }

        if (overwrite || string.IsNullOrWhiteSpace(inscription.FonctionSnapshot))
        {
            inscription.FonctionSnapshot = string.IsNullOrWhiteSpace(scout.Fonction) ? null : scout.Fonction.Trim();
        }
    }

    private static string BuildYearLabel(int year) => $"{year}-{year + 1}";

    private static string NormalizeYearLabel(int year, string? currentValue)
    {
        return string.IsNullOrWhiteSpace(currentValue) ? BuildYearLabel(year) : currentValue.Trim();
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
