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
public class RapportsActiviteController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IFileUploadService fileUploadService,
    OperationalAccessService accessService) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(Guid? groupeId, StatutWorkflowDocument? statut)
    {
        var (page, ps) = ListPagination.Read(Request);
        var isAdmin = accessService.IsAdminLike(User);
        var isSupervision = accessService.IsSupervision(User);
        var isDistrictReviewer = await accessService.IsDistrictReviewerAsync(User);
        var currentScout = await accessService.GetCurrentScoutAsync(User);
        var canCreate = isAdmin || await accessService.IsLeadershipScoutAsync(User);

        var query = db.RapportsActivite.AsNoTracking()
            .Include(r => r.Activite).ThenInclude(a => a.Groupe)
            .Include(r => r.Createur)
            .Include(r => r.Valideur)
            .Include(r => r.PiecesJointes)
            .AsQueryable();

        if (!isAdmin && !isSupervision && !isDistrictReviewer)
        {
            if (currentScout?.GroupeId is null || !OperationalAccessService.IsLeadershipFunction(currentScout.Fonction))
            {
                return Forbid();
            }

            query = query.Where(r => r.Activite.GroupeId == currentScout.GroupeId.Value);
            groupeId ??= currentScout.GroupeId;
        }

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
        ViewBag.CanCreateReport = canCreate;
        ViewBag.CanEditReport = canCreate || isAdmin;
        ViewBag.CanValidateDistrict = isDistrictReviewer;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(rapports);
    }

    public async Task<IActionResult> Create(Guid? activiteId)
    {
        var (allowed, currentScout) = await ResolveLeadershipScopeAsync();
        if (!allowed)
        {
            return Forbid();
        }

        await LoadActivitesAsync(activiteId, currentScout);
        var selectedActivite = activiteId.HasValue && activiteId.Value != Guid.Empty
            ? await db.Activites.Include(a => a.Participants).FirstOrDefaultAsync(a => a.Id == activiteId.Value)
            : null;

        return View("Upsert", new RapportActivite
        {
            ActiviteId = activiteId ?? Guid.Empty,
            DateRealisation = selectedActivite?.DateFin ?? selectedActivite?.DateDebut ?? DateTime.UtcNow.Date,
            NombreParticipants = selectedActivite?.Participants.Count ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RapportActivite model, List<IFormFile>? piecesJointes)
    {
        var (allowed, currentScout) = await ResolveLeadershipScopeAsync();
        if (!allowed)
        {
            return Forbid();
        }

        NormalizeModel(model);
        await ValidateModelAsync(model, currentScout);
        if (!ModelState.IsValid)
        {
            await LoadActivitesAsync(model.ActiviteId, currentScout);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.CreateurId = CurrentUserId;
        model.DateCreation = DateTime.UtcNow;
        db.RapportsActivite.Add(model);
        await AddAttachmentsAsync(model, piecesJointes);
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
            .Include(r => r.PiecesJointes)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (!await CanAccessReportAsync(rapport.Activite.GroupeId))
        {
            return Forbid();
        }

        ViewBag.CanManage = await CanEditReportAsync(rapport);
        ViewBag.CanValidateDistrict = await accessService.IsDistrictReviewerAsync(User);
        return View(rapport);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var rapport = await db.RapportsActivite
            .Include(r => r.Activite).ThenInclude(a => a.Groupe)
            .Include(r => r.PiecesJointes)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (!await CanEditReportAsync(rapport))
        {
            return Forbid();
        }
        if (rapport.Statut == StatutWorkflowDocument.Valide)
        {
            TempData["Error"] = "Un rapport valide ne peut plus etre modifie.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentScout = await accessService.GetCurrentScoutAsync(User);
        await LoadActivitesAsync(rapport.ActiviteId, currentScout);
        return View("Upsert", rapport);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RapportActivite model, List<IFormFile>? piecesJointes, Guid[]? piecesASupprimer)
    {
        var rapport = await db.RapportsActivite
            .Include(r => r.Activite)
            .Include(r => r.PiecesJointes)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (!await CanEditReportAsync(rapport))
        {
            return Forbid();
        }

        NormalizeModel(model);
        var currentScout = await accessService.GetCurrentScoutAsync(User);
        await ValidateModelAsync(model, currentScout, id);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.PiecesJointes = rapport.PiecesJointes;
            await LoadActivitesAsync(model.ActiviteId, currentScout);
            return View("Upsert", model);
        }

        rapport.ActiviteId = model.ActiviteId;
        rapport.DateRealisation = model.DateRealisation;
        rapport.NombreParticipants = model.NombreParticipants;
        rapport.ResumeExecutif = model.ResumeExecutif;
        rapport.ResultatsObtenus = model.ResultatsObtenus;
        rapport.DifficultesRencontrees = model.DifficultesRencontrees;
        rapport.Recommandations = model.Recommandations;
        rapport.ObservationsComplementaires = model.ObservationsComplementaires;
        if (rapport.Statut == StatutWorkflowDocument.AReviser)
        {
            rapport.CommentaireValidation = rapport.CommentaireValidation;
        }

        if (piecesASupprimer is not null && piecesASupprimer.Length != 0)
        {
            var pieces = rapport.PiecesJointes.Where(p => piecesASupprimer.Contains(p.Id)).ToList();
            if (pieces.Count != 0)
            {
                db.RapportsActivitePiecesJointes.RemoveRange(pieces);
            }
        }

        await AddAttachmentsAsync(rapport, piecesJointes);
        await db.SaveChangesAsync();
        TempData["Success"] = "Rapport d'activite mis a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (!await CanEditReportAsync(rapport))
        {
            return Forbid();
        }
        if (rapport.Statut is not (StatutWorkflowDocument.Brouillon or StatutWorkflowDocument.AReviser)) return BadRequest();

        rapport.Statut = StatutWorkflowDocument.Soumis;
        rapport.DateSoumission = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Rapport soumis au commissaire de district.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRevision(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

        var rapport = await db.RapportsActivite.FirstOrDefaultAsync(r => r.Id == id);
        if (rapport is null) return NotFound();
        if (rapport.Statut != StatutWorkflowDocument.Soumis) return BadRequest();

        rapport.Statut = StatutWorkflowDocument.AReviser;
        rapport.CommentaireValidation = string.IsNullOrWhiteSpace(commentaire) ? "Corrections demandees par le commissaire de district." : commentaire.Trim();
        rapport.DateValidation = null;
        rapport.ValideurId = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Le rapport a ete renvoye pour correction.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        if (!await accessService.IsDistrictReviewerAsync(User) && !accessService.IsAdminLike(User)) return Forbid();

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

    private async Task ValidateModelAsync(RapportActivite model, Scout? currentScout, Guid? currentId = null)
    {
        if (model.DateRealisation == default)
        {
            ModelState.AddModelError(nameof(model.DateRealisation), "La date de realisation est obligatoire.");
        }

        if (model.NombreParticipants < 0)
        {
            ModelState.AddModelError(nameof(model.NombreParticipants), "Le nombre de participants ne peut pas etre negatif.");
        }

        var activite = await db.Activites
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == model.ActiviteId && !a.EstSupprime);
        if (activite is null)
        {
            ModelState.AddModelError(nameof(model.ActiviteId), "L'activite selectionnee est introuvable.");
            return;
        }

        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId != activite.GroupeId)
        {
            ModelState.AddModelError(nameof(model.ActiviteId), "Vous ne pouvez declarer un rapport que pour une activite de votre groupe.");
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

    private async Task LoadActivitesAsync(Guid? selectedActiviteId, Scout? currentScout)
    {
        var query = db.Activites
            .Include(a => a.Groupe)
            .Include(a => a.Participants)
            .Where(a => !a.EstSupprime && (a.Statut == StatutActivite.Terminee || a.Statut == StatutActivite.Archivee))
            .AsQueryable();

        if (!accessService.IsAdminLike(User) && currentScout?.GroupeId is Guid groupeId)
        {
            query = query.Where(a => a.GroupeId == groupeId);
        }

        ViewBag.Activites = await query
            .OrderByDescending(a => a.DateFin ?? a.DateDebut)
            .ToListAsync();
        ViewBag.SelectedActiviteId = selectedActiviteId;
    }

    private async Task AddAttachmentsAsync(RapportActivite rapport, IEnumerable<IFormFile>? files)
    {
        if (files is null)
        {
            return;
        }

        foreach (var file in files.Where(f => f is not null && f.Length > 0))
        {
            var url = await fileUploadService.SaveFileAsync(file, "rapports-activite");
            rapport.PiecesJointes.Add(new RapportActivitePieceJointe
            {
                Id = Guid.NewGuid(),
                NomFichier = Path.GetFileName(file.FileName),
                UrlFichier = url,
                TypeMime = file.ContentType
            });
        }
    }

    private async Task<(bool Allowed, Scout? CurrentScout)> ResolveLeadershipScopeAsync()
    {
        if (accessService.IsAdminLike(User))
        {
            return (true, await accessService.GetCurrentScoutAsync(User));
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        return scout is not null && OperationalAccessService.IsLeadershipFunction(scout.Fonction)
            ? (true, scout)
            : (false, scout);
    }

    private async Task<bool> CanAccessReportAsync(Guid? groupeId)
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

    private async Task<bool> CanEditReportAsync(RapportActivite rapport)
    {
        if (accessService.IsAdminLike(User))
        {
            return true;
        }

        return rapport.CreateurId == CurrentUserId;
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
