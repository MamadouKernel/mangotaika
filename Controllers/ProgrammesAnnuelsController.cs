using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize]
public class ProgrammesAnnuelsController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    OperationalAccessService accessService) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(int? annee, Guid? groupeId, StatutWorkflowDocument? statut)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var (page, ps) = ListPagination.Read(Request);
        var isAdmin = accessService.IsAdminLike(User);
        var isSupervision = accessService.IsSupervision(User);
        var isDistrictReviewer = await accessService.IsDistrictReviewerAsync(User);
        var currentScout = await accessService.GetCurrentScoutAsync(User);
        var canCreate = isAdmin || await accessService.IsLeadershipScoutAsync(User);

        var query = db.ProgrammesAnnuels.AsNoTracking()
            .Include(p => p.Groupe)
            .Include(p => p.Createur)
            .Include(p => p.Valideur)
            .Include(p => p.Activites)
                .ThenInclude(a => a.Branche)
            .Where(p => p.AnneeReference == year)
            .AsQueryable();

        if (!isAdmin && !isSupervision && !isDistrictReviewer)
        {
            if (currentScout?.GroupeId is null || !OperationalAccessService.IsLeadershipFunction(currentScout.Fonction))
            {
                return Forbid();
            }

            query = query.Where(p => p.GroupeId == currentScout.GroupeId);
            groupeId ??= currentScout.GroupeId;
        }

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            query = query.Where(p => p.GroupeId == groupeId.Value);
        }

        if (statut.HasValue)
        {
            query = query.Where(p => p.Statut == statut.Value);
        }

        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var programmes = await query
            .OrderBy(x => x.Groupe != null ? x.Groupe.Nom : "District")
            .ThenBy(x => x.Titre)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Annee = year;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedStatut = statut;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.CanCreateProgram = canCreate;
        ViewBag.CanManagePrograms = canCreate || isAdmin;
        ViewBag.CanValidateDistrict = isDistrictReviewer;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(programmes);
    }

    public async Task<IActionResult> Calendrier(int? annee, Guid? groupeId)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var isAdmin = accessService.IsAdminLike(User);
        var isSupervision = accessService.IsSupervision(User);
        var isDistrictReviewer = await accessService.IsDistrictReviewerAsync(User);
        var currentScout = await accessService.GetCurrentScoutAsync(User);

        var query = db.ProgrammesAnnuelsActivites.AsNoTracking()
            .Include(a => a.Branche)
            .Include(a => a.ProgrammeAnnuel)
                .ThenInclude(p => p.Groupe)
            .Where(a => a.DateActivite.Year == year)
            .AsQueryable();

        if (!isAdmin && !isSupervision && !isDistrictReviewer)
        {
            if (currentScout?.GroupeId is null || !OperationalAccessService.IsLeadershipFunction(currentScout.Fonction))
            {
                return Forbid();
            }

            query = query.Where(a => a.ProgrammeAnnuel.GroupeId == currentScout.GroupeId);
            groupeId ??= currentScout.GroupeId;
        }

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            query = query.Where(a => a.ProgrammeAnnuel.GroupeId == groupeId.Value);
        }

        var activites = await query
            .OrderBy(a => a.DateActivite)
            .ThenBy(a => a.OrdreAffichage)
            .ToListAsync();

        ViewBag.Annee = year;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        return View(activites);
    }

    public async Task<IActionResult> Create(Guid? groupeId)
    {
        var (allowed, currentScout) = await ResolveLeadershipScopeAsync(groupeId);
        if (!allowed)
        {
            return Forbid();
        }

        var programme = new ProgrammeAnnuel
        {
            AnneeReference = DateTime.UtcNow.Year,
            Titre = "Programme annuel d'activites",
            GroupeId = groupeId,
            Activites =
            [
                new ProgrammeAnnuelActivite
                {
                    DateActivite = DateTime.UtcNow.Date,
                    OrdreAffichage = 1
                }
            ]
        };

        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId is Guid scopedGroupId)
        {
            programme.GroupeId = scopedGroupId;
        }

        await LoadReferenceDataAsync(programme.GroupeId, currentScout);
        return View("Upsert", programme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgrammeAnnuel model)
    {
        var (allowed, currentScout) = await ResolveLeadershipScopeAsync(model.GroupeId);
        if (!allowed)
        {
            return Forbid();
        }

        NormalizeModel(model);
        await ValidateModelAsync(model, currentScout);
        if (!ModelState.IsValid)
        {
            await LoadReferenceDataAsync(model.GroupeId, currentScout);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.CreateurId = CurrentUserId;
        model.DateCreation = DateTime.UtcNow;
        model.CalendrierSynthese = BuildCalendrierSynthese(model.Activites);
        PrepareActivitiesForPersistence(model);

        db.ProgrammesAnnuels.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel cree.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var programme = await db.ProgrammesAnnuels.AsNoTracking()
            .Include(p => p.Groupe)
            .Include(p => p.Createur)
            .Include(p => p.Valideur)
            .Include(p => p.Activites.OrderBy(a => a.DateActivite).ThenBy(a => a.OrdreAffichage))
                .ThenInclude(a => a.Branche)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();

        var canView = await CanAccessProgrammeAsync(programme.GroupeId);
        if (!canView)
        {
            return Forbid();
        }

        ViewBag.CanManageProgram = await CanEditProgrammeAsync(programme);
        ViewBag.CanValidateDistrict = await accessService.IsDistrictReviewerAsync(User);
        return View(programme);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var programme = await db.ProgrammesAnnuels
            .Include(p => p.Activites.OrderBy(a => a.DateActivite).ThenBy(a => a.OrdreAffichage))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (!await CanEditProgrammeAsync(programme))
        {
            return Forbid();
        }

        var currentScout = await accessService.GetCurrentScoutAsync(User);
        await LoadReferenceDataAsync(programme.GroupeId, currentScout);
        return View("Upsert", programme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProgrammeAnnuel model)
    {
        var programme = await db.ProgrammesAnnuels
            .Include(p => p.Activites)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (!await CanEditProgrammeAsync(programme))
        {
            return Forbid();
        }

        NormalizeModel(model);
        var currentScout = await accessService.GetCurrentScoutAsync(User);
        await ValidateModelAsync(model, currentScout, id);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadReferenceDataAsync(model.GroupeId, currentScout);
            return View("Upsert", model);
        }

        programme.GroupeId = model.GroupeId;
        programme.AnneeReference = model.AnneeReference;
        programme.Titre = model.Titre;
        programme.Objectifs = model.Objectifs;
        programme.Observations = model.Observations;
        programme.CalendrierSynthese = BuildCalendrierSynthese(model.Activites);
        programme.Statut = programme.Statut == StatutWorkflowDocument.AReviser ? StatutWorkflowDocument.AReviser : programme.Statut;

        db.ProgrammesAnnuelsActivites.RemoveRange(programme.Activites);
        programme.Activites.Clear();
        PrepareActivitiesForPersistence(model, programme.Id);
        foreach (var activite in model.Activites)
        {
            programme.Activites.Add(activite);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel mis a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var programme = await db.ProgrammesAnnuels
            .Include(p => p.Activites)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (!await CanEditProgrammeAsync(programme))
        {
            return Forbid();
        }

        db.ProgrammesAnnuelsActivites.RemoveRange(programme.Activites);
        db.ProgrammesAnnuels.Remove(programme);
        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel supprime.";
        return RedirectToAction(nameof(Index), new { annee = programme.AnneeReference, groupeId = programme.GroupeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (!await CanEditProgrammeAsync(programme))
        {
            return Forbid();
        }
        if (programme.Statut is not (StatutWorkflowDocument.Brouillon or StatutWorkflowDocument.AReviser or StatutWorkflowDocument.Rejete)) return BadRequest();

        programme.Statut = StatutWorkflowDocument.Soumis;
        programme.DateSoumission = DateTime.UtcNow;
        programme.CommentaireValidation = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel soumis au district.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRevision(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (programme.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        programme.Statut = StatutWorkflowDocument.AReviser;
        programme.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Modifications demandees par le district." : commentaire.Trim();
        programme.DateValidation = null;
        programme.ValideurId = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Le programme a ete renvoye pour correction.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (programme.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        programme.Statut = StatutWorkflowDocument.Rejete;
        programme.DateValidation = DateTime.UtcNow;
        programme.ValideurId = CurrentUserId;
        programme.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Programme rejete par le district." : commentaire.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "Le programme annuel a ete rejete.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (programme.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        programme.Statut = StatutWorkflowDocument.Valide;
        programme.DateValidation = DateTime.UtcNow;
        programme.ValideurId = CurrentUserId;
        programme.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel valide.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateModelAsync(ProgrammeAnnuel model, Scout? currentScout, Guid? currentId = null)
    {
        if (model.AnneeReference < 2000 || model.AnneeReference > 2100)
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "L'annee de reference est invalide.");
        }

        if (model.GroupeId.HasValue && model.GroupeId.Value != Guid.Empty)
        {
            if (!await db.Groupes.AnyAsync(g => g.Id == model.GroupeId.Value && g.IsActive))
            {
                ModelState.AddModelError(nameof(model.GroupeId), "L'entite selectionnee est introuvable ou inactive.");
            }
        }
        else if (!accessService.IsAdminLike(User))
        {
            ModelState.AddModelError(nameof(model.GroupeId), "Le groupe est obligatoire pour ce profil.");
        }

        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId != model.GroupeId)
        {
            ModelState.AddModelError(nameof(model.GroupeId), "Vous ne pouvez preparer qu'un programme annuel pour votre groupe.");
        }

        var duplicateQuery = db.ProgrammesAnnuels.AsQueryable().Where(p => p.Id != currentId && p.AnneeReference == model.AnneeReference);
        duplicateQuery = model.GroupeId.HasValue && model.GroupeId.Value != Guid.Empty
            ? duplicateQuery.Where(p => p.GroupeId == model.GroupeId.Value)
            : duplicateQuery.Where(p => p.GroupeId == null);
        if (await duplicateQuery.AnyAsync())
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "Un programme annuel existe deja pour cette entite sur cette annee.");
        }

        var activites = model.Activites.ToList();
        if (activites.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Activites), "Ajoutez au moins une activite au programme annuel.");
            return;
        }

        var branchIds = activites.Where(a => a.BrancheId.HasValue).Select(a => a.BrancheId!.Value).Distinct().ToList();
        var branches = branchIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await db.Branches.Where(b => branchIds.Contains(b.Id) && b.IsActive).ToDictionaryAsync(b => b.Id, b => b.GroupeId);

        for (var index = 0; index < activites.Count; index++)
        {
            var activite = activites[index];
            var prefix = $"Activites[{index}]";

            if (string.IsNullOrWhiteSpace(activite.NomActivite))
            {
                ModelState.AddModelError($"{prefix}.NomActivite", "Le nom de l'activite est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(activite.Objectif))
            {
                ModelState.AddModelError($"{prefix}.Objectif", "L'objectif est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(activite.Responsable))
            {
                ModelState.AddModelError($"{prefix}.Responsable", "Le responsable est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(activite.Description))
            {
                ModelState.AddModelError($"{prefix}.Description", "La description est obligatoire.");
            }

            if (activite.DateActivite == default)
            {
                ModelState.AddModelError($"{prefix}.DateActivite", "La date de l'activite est obligatoire.");
            }
            else if (activite.DateActivite.Year != model.AnneeReference)
            {
                ModelState.AddModelError($"{prefix}.DateActivite", "La date de l'activite doit appartenir a l'annee de reference.");
            }

            if (activite.BrancheId.HasValue)
            {
                if (!model.GroupeId.HasValue)
                {
                    ModelState.AddModelError($"{prefix}.BrancheId", "Le groupe doit etre renseigne pour associer une branche.");
                }
                else if (!branches.TryGetValue(activite.BrancheId.Value, out var groupeBranche))
                {
                    ModelState.AddModelError($"{prefix}.BrancheId", "La branche selectionnee est introuvable ou inactive.");
                }
                else if (groupeBranche != model.GroupeId.Value)
                {
                    ModelState.AddModelError($"{prefix}.BrancheId", "La branche selectionnee doit appartenir au groupe du programme.");
                }
            }
        }
    }

    private async Task LoadReferenceDataAsync(Guid? selectedGroupeId, Scout? currentScout)
    {
        var groupesQuery = db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).AsQueryable();
        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId is Guid scopedGroupId)
        {
            groupesQuery = groupesQuery.Where(g => g.Id == scopedGroupId);
            selectedGroupeId ??= scopedGroupId;
        }

        ViewBag.Groupes = await groupesQuery.ToListAsync();
        ViewBag.Branches = await db.Branches.Where(b => b.IsActive).OrderBy(b => b.Nom).ToListAsync();
        ViewBag.SelectedGroupeId = selectedGroupeId;
    }

    private async Task<(bool Allowed, Scout? CurrentScout)> ResolveLeadershipScopeAsync(Guid? groupeId)
    {
        if (accessService.IsAdminLike(User))
        {
            return (true, await accessService.GetCurrentScoutAsync(User));
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        if (scout is null || !OperationalAccessService.IsLeadershipFunction(scout.Fonction))
        {
            return (false, scout);
        }

        if (groupeId.HasValue && groupeId.Value != Guid.Empty && scout.GroupeId != groupeId)
        {
            return (false, scout);
        }

        return (true, scout);
    }

    private async Task<bool> CanAccessProgrammeAsync(Guid? groupeId)
    {
        if (accessService.IsAdminLike(User) || accessService.IsSupervision(User) || await accessService.IsDistrictReviewerAsync(User))
        {
            return true;
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        return scout is not null
            && OperationalAccessService.IsLeadershipFunction(scout.Fonction)
            && scout.GroupeId == groupeId;
    }

    private async Task<bool> CanEditProgrammeAsync(ProgrammeAnnuel programme)
    {
        if (accessService.IsAdminLike(User))
        {
            return true;
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        return scout is not null
            && OperationalAccessService.IsLeadershipFunction(scout.Fonction)
            && scout.GroupeId == programme.GroupeId;
    }

    private static void NormalizeModel(ProgrammeAnnuel model)
    {
        model.GroupeId = model.GroupeId == Guid.Empty ? null : model.GroupeId;
        model.Titre = model.Titre?.Trim() ?? string.Empty;
        model.Objectifs = model.Objectifs?.Trim() ?? string.Empty;
        model.Observations = string.IsNullOrWhiteSpace(model.Observations) ? null : model.Observations.Trim();
        model.Activites = (model.Activites ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.NomActivite)
                || !string.IsNullOrWhiteSpace(a.Objectif)
                || !string.IsNullOrWhiteSpace(a.Responsable)
                || !string.IsNullOrWhiteSpace(a.Description)
                || a.BrancheId.HasValue
                || !string.IsNullOrWhiteSpace(a.Cible)
                || !string.IsNullOrWhiteSpace(a.Lieu)
                || a.DateActivite != default
                || a.MontantParticipation.HasValue)
            .Select((a, index) => new ProgrammeAnnuelActivite
            {
                Id = a.Id,
                BrancheId = a.BrancheId == Guid.Empty ? null : a.BrancheId,
                Cible = string.IsNullOrWhiteSpace(a.Cible) ? null : a.Cible.Trim(),
                NomActivite = a.NomActivite?.Trim() ?? string.Empty,
                Objectif = a.Objectif?.Trim() ?? string.Empty,
                Lieu = string.IsNullOrWhiteSpace(a.Lieu) ? null : a.Lieu.Trim(),
                DateActivite = a.DateActivite == default ? DateTime.UtcNow.Date : a.DateActivite.Date,
                Responsable = a.Responsable?.Trim() ?? string.Empty,
                Description = a.Description?.Trim() ?? string.Empty,
                MontantParticipation = a.MontantParticipation,
                OrdreAffichage = index + 1
            })
            .ToList();
    }

    private static void PrepareActivitiesForPersistence(ProgrammeAnnuel model, Guid? programmeId = null)
    {
        foreach (var activite in model.Activites.Select((value, index) => (value, index)))
        {
            activite.value.Id = Guid.NewGuid();
            activite.value.ProgrammeAnnuelId = programmeId ?? model.Id;
            activite.value.OrdreAffichage = activite.index + 1;
        }
    }

    private static string BuildCalendrierSynthese(IEnumerable<ProgrammeAnnuelActivite> activites)
    {
        return string.Join(Environment.NewLine,
            activites
                .OrderBy(a => a.DateActivite)
                .ThenBy(a => a.OrdreAffichage)
                .Select(a => $"{a.DateActivite:dd/MM/yyyy} - {a.NomActivite} - {a.Responsable}"));
    }
}

