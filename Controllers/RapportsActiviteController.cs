using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class RapportsActiviteController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(Guid? groupeId, StatutWorkflowDocument? statut)
    {
        var (page, ps) = ListPagination.Read(Request);

        var query = db.RapportsActivite.AsNoTracking()
            .Include(r => r.Activite).ThenInclude(a => a.Groupe)
            .Include(r => r.Createur)
            .Include(r => r.Valideur)
            .AsQueryable();

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            query = query.Where(r => r.Activite.GroupeId == groupeId.Value);
        }

        if (statut.HasValue)
        {
            query = query.Where(r => r.Statut == statut.Value);
        }

        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var rapports = await query
            .OrderByDescending(r => r.DateCreation)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedStatut = statut;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(rapports);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(Guid? activiteId)
    {
        await LoadActivitesAsync(activiteId);
        return View("Upsert", new RapportActivite
        {
            ActiviteId = activiteId ?? Guid.Empty
        });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RapportActivite model)
    {
        NormalizeModel(model);
        await ValidateModelAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadActivitesAsync(model.ActiviteId);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.CreateurId = CurrentUserId;
        model.DateCreation = DateTime.UtcNow;
        db.RapportsActivite.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Rapport d'activite cree.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var rapport = await db.RapportsActivite.AsNoTracking()
            .Include(r => r.Activite).ThenInclude(a => a.Groupe)
            .Include(r => r.Createur)
            .Include(r => r.Valideur)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        return View(rapport);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        await LoadActivitesAsync(rapport.ActiviteId);
        return View("Upsert", rapport);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RapportActivite model)
    {
        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();

        NormalizeModel(model);
        await ValidateModelAsync(model, id);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadActivitesAsync(model.ActiviteId);
            return View("Upsert", model);
        }

        rapport.ActiviteId = model.ActiviteId;
        rapport.ResumeExecutif = model.ResumeExecutif;
        rapport.ResultatsObtenus = model.ResultatsObtenus;
        rapport.DifficultesRencontrees = model.DifficultesRencontrees;
        rapport.Recommandations = model.Recommandations;
        rapport.ObservationsComplementaires = model.ObservationsComplementaires;
        await db.SaveChangesAsync();
        TempData["Success"] = "Rapport d'activite mis a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (rapport.Statut is not (StatutWorkflowDocument.Brouillon or StatutWorkflowDocument.AReviser)) return BadRequest();

        rapport.Statut = StatutWorkflowDocument.Soumis;
        rapport.DateSoumission = DateTime.UtcNow;
        rapport.CommentaireValidation = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Rapport soumis pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRevision(Guid id, string? commentaire)
    {
        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (rapport.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        rapport.Statut = StatutWorkflowDocument.AReviser;
        rapport.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Corrections demandees." : commentaire.Trim();
        rapport.DateValidation = null;
        rapport.ValideurId = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Le rapport a ete renvoye en revision.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (rapport.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        rapport.Statut = StatutWorkflowDocument.Valide;
        rapport.DateValidation = DateTime.UtcNow;
        rapport.ValideurId = CurrentUserId;
        rapport.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "Rapport d'activite valide.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateModelAsync(RapportActivite model, Guid? currentId = null)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == model.ActiviteId && !a.EstSupprime);
        if (activite is null)
        {
            ModelState.AddModelError(nameof(model.ActiviteId), "L'activite selectionnee est introuvable.");
            return;
        }

        if (activite.Statut is not (StatutActivite.Terminee or StatutActivite.Archivee))
        {
            ModelState.AddModelError(nameof(model.ActiviteId), "Le rapport d'activite ne peut etre redige qu'apres cloture de l'activite.");
        }

        if (await db.RapportsActivite.AnyAsync(r => r.Id != currentId && r.ActiviteId == model.ActiviteId))
        {
            ModelState.AddModelError(nameof(model.ActiviteId), "Un rapport existe deja pour cette activite.");
        }
    }

    private async Task LoadActivitesAsync(Guid? selectedActiviteId)
    {
        ViewBag.Activites = await db.Activites
            .Include(a => a.Groupe)
            .Where(a => !a.EstSupprime && (a.Statut == StatutActivite.Terminee || a.Statut == StatutActivite.Archivee))
            .OrderByDescending(a => a.DateFin ?? a.DateDebut)
            .ToListAsync();
        ViewBag.SelectedActiviteId = selectedActiviteId;
    }

    private static void NormalizeModel(RapportActivite model)
    {
        model.ResumeExecutif = model.ResumeExecutif?.Trim() ?? string.Empty;
        model.ResultatsObtenus = model.ResultatsObtenus?.Trim() ?? string.Empty;
        model.DifficultesRencontrees = model.DifficultesRencontrees?.Trim() ?? string.Empty;
        model.Recommandations = model.Recommandations?.Trim() ?? string.Empty;
        model.ObservationsComplementaires = string.IsNullOrWhiteSpace(model.ObservationsComplementaires) ? null : model.ObservationsComplementaires.Trim();
    }
}
