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
public class PropositionsMaitriseController(
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

        var query = db.PropositionsMaitriseAnnuelles.AsNoTracking()
            .Include(p => p.Groupe)
            .Include(p => p.Createur)
            .Include(p => p.Valideur)
            .Include(p => p.Membres.OrderBy(m => m.OrdreAffichage))
                .ThenInclude(m => m.Branche)
            .Where(p => p.AnneeReference == year)
            .AsQueryable();

        if (!isAdmin && !isSupervision && !isDistrictReviewer)
        {
            if (currentScout?.GroupeId is null || !OperationalAccessService.IsLeadershipFunction(currentScout.Fonction))
            {
                return Forbid();
            }

            query = query.Where(p => p.GroupeId == currentScout.GroupeId.Value);
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
        var propositions = await query
            .OrderBy(x => x.Groupe.Nom)
            .ThenBy(x => x.Titre)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Annee = year;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedStatut = statut;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.CanCreateLeadershipPlan = canCreate;
        ViewBag.CanManageLeadershipPlan = canCreate || isAdmin;
        ViewBag.CanValidateDistrict = isDistrictReviewer;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(propositions);
    }

    public async Task<IActionResult> Create(Guid? groupeId)
    {
        var (allowed, currentScout) = await ResolveLeadershipScopeAsync(groupeId);
        if (!allowed)
        {
            return Forbid();
        }

        var proposition = new PropositionMaitriseAnnuelle
        {
            GroupeId = groupeId ?? Guid.Empty,
            AnneeReference = DateTime.UtcNow.Year,
            Titre = "Proposition annuelle de maitrise",
            Membres =
            [
                new PropositionMaitriseMembre { OrdreAffichage = 1 }
            ]
        };

        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId is Guid scopedGroupId)
        {
            proposition.GroupeId = scopedGroupId;
        }

        await LoadReferenceDataAsync(proposition.GroupeId, currentScout);
        return View("Upsert", proposition);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropositionMaitriseAnnuelle model)
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
        model.CompositionProposee = BuildCompositionSummary(model.Membres);
        PrepareMembersForPersistence(model);

        db.PropositionsMaitriseAnnuelles.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Proposition de maitrise creee.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles.AsNoTracking()
            .Include(p => p.Groupe)
            .Include(p => p.Createur)
            .Include(p => p.Valideur)
            .Include(p => p.Membres.OrderBy(m => m.OrdreAffichage))
                .ThenInclude(m => m.Branche)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();

        if (!await CanAccessProposalAsync(proposition.GroupeId))
        {
            return Forbid();
        }

        ViewBag.CanManageLeadershipPlan = await CanEditProposalAsync(proposition);
        ViewBag.CanValidateDistrict = await accessService.IsDistrictReviewerAsync(User);
        return View(proposition);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles
            .Include(p => p.Membres.OrderBy(m => m.OrdreAffichage))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (!await CanEditProposalAsync(proposition))
        {
            return Forbid();
        }

        var currentScout = await accessService.GetCurrentScoutAsync(User);
        await LoadReferenceDataAsync(proposition.GroupeId, currentScout);
        return View("Upsert", proposition);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PropositionMaitriseAnnuelle model)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles
            .Include(p => p.Membres)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (!await CanEditProposalAsync(proposition))
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

        proposition.GroupeId = model.GroupeId;
        proposition.AnneeReference = model.AnneeReference;
        proposition.Titre = model.Titre;
        proposition.ObjectifsPedagogiques = model.ObjectifsPedagogiques;
        proposition.BesoinsFormation = model.BesoinsFormation;
        proposition.Observations = model.Observations;
        proposition.CompositionProposee = BuildCompositionSummary(model.Membres);
        proposition.Statut = proposition.Statut == StatutWorkflowDocument.AReviser ? StatutWorkflowDocument.AReviser : proposition.Statut;

        db.PropositionsMaitriseMembres.RemoveRange(proposition.Membres);
        proposition.Membres.Clear();
        PrepareMembersForPersistence(model, proposition.Id);
        foreach (var membre in model.Membres)
        {
            proposition.Membres.Add(membre);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Proposition de maitrise mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (!await CanEditProposalAsync(proposition))
        {
            return Forbid();
        }
        if (proposition.Statut is not (StatutWorkflowDocument.Brouillon or StatutWorkflowDocument.AReviser or StatutWorkflowDocument.Rejete)) return BadRequest();

        proposition.Statut = StatutWorkflowDocument.Soumis;
        proposition.DateSoumission = DateTime.UtcNow;
        proposition.CommentaireValidation = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Proposition de maitrise soumise au district.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRevision(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (proposition.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        proposition.Statut = StatutWorkflowDocument.AReviser;
        proposition.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Modifications demandees par le district." : commentaire.Trim();
        proposition.DateValidation = null;
        proposition.ValideurId = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "La proposition a ete renvoyee pour correction.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (proposition.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        proposition.Statut = StatutWorkflowDocument.Rejete;
        proposition.DateValidation = DateTime.UtcNow;
        proposition.ValideurId = CurrentUserId;
        proposition.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Proposition rejetee par le district." : commentaire.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "La proposition de maitrise a ete rejetee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (proposition.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        proposition.Statut = StatutWorkflowDocument.Valide;
        proposition.DateValidation = DateTime.UtcNow;
        proposition.ValideurId = CurrentUserId;
        proposition.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "Proposition de maitrise validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateModelAsync(PropositionMaitriseAnnuelle model, Scout? currentScout, Guid? currentId = null)
    {
        if (!await db.Groupes.AnyAsync(g => g.Id == model.GroupeId && g.IsActive))
        {
            ModelState.AddModelError(nameof(model.GroupeId), "L'entite selectionnee est introuvable ou inactive.");
        }

        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId != model.GroupeId)
        {
            ModelState.AddModelError(nameof(model.GroupeId), "Vous ne pouvez preparer qu'une proposition de maitrise pour votre groupe.");
        }

        if (await db.PropositionsMaitriseAnnuelles.AnyAsync(p => p.Id != currentId && p.GroupeId == model.GroupeId && p.AnneeReference == model.AnneeReference))
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "Une proposition annuelle de maitrise existe deja pour cette entite sur cette annee.");
        }

        var membres = model.Membres.ToList();
        if (membres.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Membres), "Ajoutez au moins un chef a la proposition de maitrise.");
            return;
        }

        var branchIds = membres.Where(m => m.BrancheId.HasValue).Select(m => m.BrancheId!.Value).Distinct().ToList();
        var branches = branchIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await db.Branches.Where(b => branchIds.Contains(b.Id) && b.IsActive).ToDictionaryAsync(b => b.Id, b => b.GroupeId);

        for (var index = 0; index < membres.Count; index++)
        {
            var membre = membres[index];
            var prefix = $"Membres[{index}]";

            if (string.IsNullOrWhiteSpace(membre.NomChef))
            {
                ModelState.AddModelError($"{prefix}.NomChef", "Le nom du chef est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(membre.Fonction))
            {
                ModelState.AddModelError($"{prefix}.Fonction", "La fonction est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(membre.Contact))
            {
                ModelState.AddModelError($"{prefix}.Contact", "Le contact est obligatoire.");
            }

            if (membre.BrancheId.HasValue)
            {
                if (!branches.TryGetValue(membre.BrancheId.Value, out var groupeBranche))
                {
                    ModelState.AddModelError($"{prefix}.BrancheId", "La branche selectionnee est introuvable ou inactive.");
                }
                else if (groupeBranche != model.GroupeId)
                {
                    ModelState.AddModelError($"{prefix}.BrancheId", "La branche selectionnee doit appartenir au groupe de la proposition.");
                }
            }
        }
    }

    private async Task LoadReferenceDataAsync(Guid selectedGroupeId, Scout? currentScout)
    {
        var groupesQuery = db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).AsQueryable();
        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId is Guid scopedGroupId)
        {
            groupesQuery = groupesQuery.Where(g => g.Id == scopedGroupId);
            selectedGroupeId = scopedGroupId;
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

    private async Task<bool> CanAccessProposalAsync(Guid groupeId)
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

    private async Task<bool> CanEditProposalAsync(PropositionMaitriseAnnuelle proposition)
    {
        if (accessService.IsAdminLike(User))
        {
            return true;
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        return scout is not null
            && OperationalAccessService.IsLeadershipFunction(scout.Fonction)
            && scout.GroupeId == proposition.GroupeId;
    }

    private static void NormalizeModel(PropositionMaitriseAnnuelle model)
    {
        model.Titre = model.Titre?.Trim() ?? string.Empty;
        model.ObjectifsPedagogiques = model.ObjectifsPedagogiques?.Trim() ?? string.Empty;
        model.BesoinsFormation = string.IsNullOrWhiteSpace(model.BesoinsFormation) ? null : model.BesoinsFormation.Trim();
        model.Observations = string.IsNullOrWhiteSpace(model.Observations) ? null : model.Observations.Trim();
        model.Membres = (model.Membres ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.NomChef)
                || !string.IsNullOrWhiteSpace(m.Fonction)
                || !string.IsNullOrWhiteSpace(m.Contact)
                || !string.IsNullOrWhiteSpace(m.NiveauFormation)
                || m.BrancheId.HasValue)
            .Select((m, index) => new PropositionMaitriseMembre
            {
                Id = m.Id,
                NomChef = m.NomChef?.Trim() ?? string.Empty,
                Fonction = m.Fonction?.Trim() ?? string.Empty,
                BrancheId = m.BrancheId == Guid.Empty ? null : m.BrancheId,
                Contact = m.Contact?.Trim() ?? string.Empty,
                NiveauFormation = string.IsNullOrWhiteSpace(m.NiveauFormation) ? null : m.NiveauFormation.Trim(),
                OrdreAffichage = index + 1
            })
            .ToList();
        model.CompositionProposee = BuildCompositionSummary(model.Membres);
    }

    private static void PrepareMembersForPersistence(PropositionMaitriseAnnuelle model, Guid? propositionId = null)
    {
        foreach (var membre in model.Membres.Select((value, index) => (value, index)))
        {
            membre.value.Id = Guid.NewGuid();
            membre.value.PropositionMaitriseAnnuelleId = propositionId ?? model.Id;
            membre.value.OrdreAffichage = membre.index + 1;
        }
    }

    private static string BuildCompositionSummary(IEnumerable<PropositionMaitriseMembre> membres)
    {
        return string.Join(Environment.NewLine,
            membres
                .OrderBy(m => m.OrdreAffichage)
                .Select(m => string.IsNullOrWhiteSpace(m.NiveauFormation)
                    ? $"{m.NomChef} - {m.Fonction}"
                    : $"{m.NomChef} - {m.Fonction} - {m.NiveauFormation}"));
    }
}

