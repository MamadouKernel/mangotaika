using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class ProgrammesAnnuelsController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(int? annee, Guid? groupeId, StatutWorkflowDocument? statut)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var (page, ps) = ListPagination.Read(Request);

        var query = db.ProgrammesAnnuels.AsNoTracking()
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
        var programmes = await query
            .OrderBy(pgm => pgm.Groupe != null ? pgm.Groupe.Nom : "District")
            .ThenBy(pgm => pgm.Titre)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Annee = year;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedStatut = statut;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(programmes);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadGroupesAsync(null);
        return View("Upsert", new ProgrammeAnnuel
        {
            AnneeReference = DateTime.UtcNow.Year,
            Titre = "Programme annuel",
            Objectifs = string.Empty,
            CalendrierSynthese = string.Empty
        });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgrammeAnnuel model)
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
            .FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        return View(programme);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        await LoadGroupesAsync(programme.GroupeId);
        return View("Upsert", programme);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProgrammeAnnuel model)
    {
        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();

        NormalizeModel(model);
        await ValidateModelAsync(model, id);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadGroupesAsync(model.GroupeId);
            return View("Upsert", model);
        }

        programme.GroupeId = model.GroupeId;
        programme.AnneeReference = model.AnneeReference;
        programme.Titre = model.Titre;
        programme.Objectifs = model.Objectifs;
        programme.CalendrierSynthese = model.CalendrierSynthese;
        programme.Observations = model.Observations;
        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel mis a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (programme.Statut is not (StatutWorkflowDocument.Brouillon or StatutWorkflowDocument.AReviser)) return BadRequest();

        programme.Statut = StatutWorkflowDocument.Soumis;
        programme.DateSoumission = DateTime.UtcNow;
        programme.CommentaireValidation = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Programme annuel soumis pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRevision(Guid id, string? commentaire)
    {
        var programme = await db.ProgrammesAnnuels.FirstOrDefaultAsync(p => p.Id == id);
        if (programme is null) return NotFound();
        if (programme.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        programme.Statut = StatutWorkflowDocument.AReviser;
        programme.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Revision demandee." : commentaire.Trim();
        programme.DateValidation = null;
        programme.ValideurId = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Le programme a ete renvoye en revision.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
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

    private async Task ValidateModelAsync(ProgrammeAnnuel model, Guid? currentId = null)
    {
        if (model.GroupeId.HasValue && model.GroupeId.Value != Guid.Empty)
        {
            if (!await db.Groupes.AnyAsync(g => g.Id == model.GroupeId.Value && g.IsActive))
            {
                ModelState.AddModelError(nameof(model.GroupeId), "L'entite selectionnee est introuvable ou inactive.");
            }
        }

        var duplicateQuery = db.ProgrammesAnnuels.AsQueryable().Where(p => p.Id != currentId && p.AnneeReference == model.AnneeReference);
        duplicateQuery = model.GroupeId.HasValue && model.GroupeId.Value != Guid.Empty
            ? duplicateQuery.Where(p => p.GroupeId == model.GroupeId.Value)
            : duplicateQuery.Where(p => p.GroupeId == null);

        if (await duplicateQuery.AnyAsync())
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "Un programme annuel existe deja pour cette entite sur cette annee.");
        }
    }

    private async Task LoadGroupesAsync(Guid? selectedGroupeId)
    {
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.SelectedGroupeId = selectedGroupeId;
    }

    private static void NormalizeModel(ProgrammeAnnuel model)
    {
        model.GroupeId = model.GroupeId == Guid.Empty ? null : model.GroupeId;
        model.Titre = model.Titre?.Trim() ?? string.Empty;
        model.Objectifs = model.Objectifs?.Trim() ?? string.Empty;
        model.CalendrierSynthese = model.CalendrierSynthese?.Trim() ?? string.Empty;
        model.Observations = string.IsNullOrWhiteSpace(model.Observations) ? null : model.Observations.Trim();
    }
}
