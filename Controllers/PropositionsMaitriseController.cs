using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class PropositionsMaitriseController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(int? annee, Guid? groupeId, StatutWorkflowDocument? statut)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var (page, ps) = ListPagination.Read(Request);

        var query = db.PropositionsMaitriseAnnuelles.AsNoTracking()
            .Include(p => p.Groupe)
            .Include(p => p.Createur)
            .Include(p => p.Valideur)
            .AsQueryable();

        query = query.Where(p => p.AnneeReference == year);

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
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(propositions);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(Guid? groupeId)
    {
        await LoadGroupesAsync(groupeId);
        return View("Upsert", new PropositionMaitriseAnnuelle
        {
            GroupeId = groupeId ?? Guid.Empty,
            AnneeReference = DateTime.UtcNow.Year,
            Titre = "Proposition annuelle de maitrise"
        });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropositionMaitriseAnnuelle model)
    {
        NormalizeModel(model);
        await ValidateModelAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadGroupesAsync(model.GroupeId);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.CreateurId = CurrentUserId;
        model.DateCreation = DateTime.UtcNow;
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
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        return View(proposition);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        await LoadGroupesAsync(proposition.GroupeId);
        return View("Upsert", proposition);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PropositionMaitriseAnnuelle model)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();

        NormalizeModel(model);
        await ValidateModelAsync(model, id);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadGroupesAsync(model.GroupeId);
            return View("Upsert", model);
        }

        proposition.GroupeId = model.GroupeId;
        proposition.AnneeReference = model.AnneeReference;
        proposition.Titre = model.Titre;
        proposition.CompositionProposee = model.CompositionProposee;
        proposition.ObjectifsPedagogiques = model.ObjectifsPedagogiques;
        proposition.BesoinsFormation = model.BesoinsFormation;
        proposition.Observations = model.Observations;
        await db.SaveChangesAsync();
        TempData["Success"] = "Proposition de maitrise mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (proposition.Statut is not (StatutWorkflowDocument.Brouillon or StatutWorkflowDocument.AReviser)) return BadRequest();

        proposition.Statut = StatutWorkflowDocument.Soumis;
        proposition.DateSoumission = DateTime.UtcNow;
        proposition.CommentaireValidation = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Proposition soumise pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRevision(Guid id, string? commentaire)
    {
        var proposition = await db.PropositionsMaitriseAnnuelles.FirstOrDefaultAsync(p => p.Id == id);
        if (proposition is null) return NotFound();
        if (proposition.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        proposition.Statut = StatutWorkflowDocument.AReviser;
        proposition.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Revision demandee." : commentaire.Trim();
        proposition.DateValidation = null;
        proposition.ValideurId = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "La proposition a ete renvoyee en revision.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
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

    private async Task ValidateModelAsync(PropositionMaitriseAnnuelle model, Guid? currentId = null)
    {
        if (!await db.Groupes.AnyAsync(g => g.Id == model.GroupeId && g.IsActive))
        {
            ModelState.AddModelError(nameof(model.GroupeId), "L'entite selectionnee est introuvable ou inactive.");
        }

        if (await db.PropositionsMaitriseAnnuelles.AnyAsync(p => p.Id != currentId && p.GroupeId == model.GroupeId && p.AnneeReference == model.AnneeReference))
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "Une proposition annuelle de maitrise existe deja pour cette entite sur cette annee.");
        }
    }

    private async Task LoadGroupesAsync(Guid? selectedGroupeId)
    {
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.SelectedGroupeId = selectedGroupeId;
    }

    private static void NormalizeModel(PropositionMaitriseAnnuelle model)
    {
        model.Titre = model.Titre?.Trim() ?? string.Empty;
        model.CompositionProposee = model.CompositionProposee?.Trim() ?? string.Empty;
        model.ObjectifsPedagogiques = model.ObjectifsPedagogiques?.Trim() ?? string.Empty;
        model.BesoinsFormation = string.IsNullOrWhiteSpace(model.BesoinsFormation) ? null : model.BesoinsFormation.Trim();
        model.Observations = string.IsNullOrWhiteSpace(model.Observations) ? null : model.Observations.Trim();
    }
}
