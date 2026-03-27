using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize]
public class FormationsController(
    IFormationService formationService,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationDispatchService notificationDispatchService) : Controller
{
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var total = await formationService.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = await formationService.GetPageAsync(skip, pageSize);
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await ChargerViewBags();
        return View(new FormationCreateDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(FormationCreateDto dto, IFormFile? image)
    {
        var user = await userManager.GetUserAsync(User);
        var formation = await formationService.CreateAsync(dto, user!.Id);

        if (image != null)
        {
            var uploadsDir = Path.Combine("wwwroot", "uploads", "formations");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"cover-{formation.Id}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);
            formation.ImageUrl = $"/uploads/formations/{fileName}";
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Formation creee avec succes.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var formation = await formationService.GetDetailAsync(id);
        if (formation is null)
            return NotFound();

        await ChargerViewBags(id);
        return View(formation);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id, FormationCreateDto dto, IFormFile? image)
    {
        await formationService.UpdateAsync(id, dto);

        if (image != null)
        {
            var uploadsDir = Path.Combine("wwwroot", "uploads", "formations");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"cover-{id}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            var formation = await db.Formations.FindAsync(id);
            if (formation != null)
            {
                formation.ImageUrl = $"/uploads/formations/{fileName}";
                await db.SaveChangesAsync();
            }
        }

        TempData["Success"] = "Formation mise a jour.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Details(Guid id)
    {
        var formation = await formationService.GetDetailAsync(id);
        if (formation is null)
            return NotFound();

        ViewBag.Stats = await formationService.GetStatsAsync(id);
        return View(formation);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await formationService.DeleteAsync(id);
        TempData["Success"] = "Formation supprimee.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Publier(Guid id)
    {
        await formationService.PublierAsync(id);
        TempData["Success"] = "Formation publiee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Archiver(Guid id)
    {
        await formationService.ArchiverAsync(id);
        TempData["Success"] = "Formation archivee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterModule(Guid formationId, ModuleCreateDto dto)
    {
        await formationService.AjouterModuleAsync(formationId, dto);
        TempData["Success"] = "Module ajoute.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> UpdateModule(Guid moduleId, Guid formationId, ModuleCreateDto dto)
    {
        await formationService.UpdateModuleAsync(moduleId, dto);
        TempData["Success"] = "Module mis a jour.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteModule(Guid moduleId, Guid formationId)
    {
        await formationService.DeleteModuleAsync(moduleId);
        TempData["Success"] = "Module supprime.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterLecon(Guid moduleId, Guid formationId, LeconCreateDto dto, IFormFile? document)
    {
        var lecon = await formationService.AjouterLeconAsync(moduleId, dto);

        if (document != null && dto.Type == TypeLecon.Document)
        {
            var uploadsDir = Path.Combine("wwwroot", "uploads", "formations");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{lecon.Id}{Path.GetExtension(document.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await document.CopyToAsync(stream);
            lecon.DocumentUrl = $"/uploads/formations/{fileName}";
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Lecon ajoutee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteLecon(Guid leconId, Guid formationId)
    {
        await formationService.DeleteLeconAsync(leconId);
        TempData["Success"] = "Lecon supprimee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> CreerQuiz(
        Guid moduleId,
        Guid formationId,
        string titre,
        int noteMinimale = 70,
        int? nombreTentativesMax = null,
        DateTime? dateOuvertureDisponibilite = null,
        DateTime? dateFermetureDisponibilite = null)
    {
        if (dateOuvertureDisponibilite.HasValue && dateFermetureDisponibilite.HasValue && dateFermetureDisponibilite < dateOuvertureDisponibilite)
        {
            TempData["Error"] = "La date de fermeture du quiz doit etre posterieure a sa date d'ouverture.";
            return RedirectToAction(nameof(Edit), new { id = formationId });
        }

        await formationService.CreerQuizAsync(
            moduleId,
            titre,
            noteMinimale,
            nombreTentativesMax,
            dateOuvertureDisponibilite,
            dateFermetureDisponibilite);
        TempData["Success"] = "Quiz cree.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> UpdateQuiz(
        Guid quizId,
        Guid formationId,
        string titre,
        int noteMinimale = 70,
        int? nombreTentativesMax = null,
        DateTime? dateOuvertureDisponibilite = null,
        DateTime? dateFermetureDisponibilite = null)
    {
        if (dateOuvertureDisponibilite.HasValue && dateFermetureDisponibilite.HasValue && dateFermetureDisponibilite < dateOuvertureDisponibilite)
        {
            TempData["Error"] = "La date de fermeture du quiz doit etre posterieure a sa date d'ouverture.";
            return RedirectToAction(nameof(Edit), new { id = formationId });
        }

        await formationService.UpdateQuizAsync(
            quizId,
            titre,
            noteMinimale,
            nombreTentativesMax,
            dateOuvertureDisponibilite,
            dateFermetureDisponibilite);
        TempData["Success"] = "Regles du quiz mises a jour.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterQuestion(Guid quizId, Guid formationId, QuestionCreateDto dto)
    {
        await formationService.AjouterQuestionAsync(quizId, dto);
        TempData["Success"] = "Question ajoutee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, Guid formationId)
    {
        await formationService.DeleteQuestionAsync(questionId);
        TempData["Success"] = "Question supprimee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteQuiz(Guid quizId, Guid formationId)
    {
        await formationService.DeleteQuizAsync(quizId);
        TempData["Success"] = "Quiz supprime.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterSession(Guid formationId, SessionFormationCreateDto dto)
    {
        if (!dto.EstSelfPaced && dto.DateOuverture.HasValue && dto.DateFermeture.HasValue && dto.DateFermeture < dto.DateOuverture)
        {
            TempData["Error"] = "La date de fermeture doit etre posterieure a la date d'ouverture.";
            return RedirectToAction(nameof(Edit), new { id = formationId });
        }

        await formationService.AjouterSessionAsync(formationId, dto);
        TempData["Success"] = "Session ajoutee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, Guid formationId)
    {
        await formationService.DeleteSessionAsync(sessionId);
        TempData["Success"] = "Session supprimee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterAnnonce(Guid formationId, AnnonceFormationCreateDto dto)
    {
        var user = await userManager.GetUserAsync(User);
        var annonce = await formationService.AjouterAnnonceAsync(formationId, dto, user?.Id);
        if (annonce.EstPubliee)
        {
            var formation = await db.Formations
                .AsNoTracking()
                .Where(f => f.Id == formationId)
                .Select(f => f.Titre)
                .FirstOrDefaultAsync();
            var recipients = await GetEnrolledLearnerUserIdsAsync(formationId, user?.Id);
            if (recipients.Count != 0)
            {
                await notificationDispatchService.SendAsync(
                    recipients,
                    "Nouvelle annonce de cours",
                    $"Nouvelle annonce dans \"{formation}\": {annonce.Titre}",
                    "LMS",
                    $"/Formations/Suivre/{formationId}");
            }
        }
        TempData["Success"] = "Annonce publiee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteAnnonce(Guid annonceId, Guid formationId)
    {
        await formationService.DeleteAnnonceAsync(annonceId);
        TempData["Success"] = "Annonce supprimee.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterJalon(Guid formationId, JalonFormationCreateDto dto)
    {
        await formationService.AjouterJalonAsync(formationId, dto);
        TempData["Success"] = "Jalon pedagogique ajoute.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> DeleteJalon(Guid jalonId, Guid formationId)
    {
        await formationService.DeleteJalonAsync(jalonId);
        TempData["Success"] = "Jalon supprime.";
        return RedirectToAction(nameof(Edit), new { id = formationId });
    }

    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> Catalogue(string? q = null, string? niveau = null, string? session = null, bool? certifiant = null)
    {
        var scout = await GetCurrentScoutAsync();
        var formations = await formationService.GetCatalogueAsync(scout?.BrancheId, scout?.Id);
        formations = ApplyCatalogueFilters(formations, q, niveau, session, certifiant);
        ViewBag.ScoutId = scout?.Id;
        ViewBag.Recherche = q;
        ViewBag.Niveau = niveau;
        ViewBag.Session = session;
        ViewBag.Certifiant = certifiant;

        if (scout != null)
        {
            var inscriptions = await formationService.GetInscriptionsScoutAsync(scout.Id);
            ViewBag.FormationsInscrites = inscriptions.Select(i => i.FormationId).ToHashSet();
        }

        return View(formations);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> Inscrire(Guid formationId)
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction(nameof(Catalogue));

        try
        {
            await formationService.InscrireScoutAsync(formationId, scout.Id);
            TempData["Success"] = "Inscription reussie.";
        }
        catch (InvalidOperationException ex)
        {
            this.SetDomainError(ex);
        }

        return RedirectToAction(nameof(MesFormations));
    }

    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> MesFormations(string? q = null, string? statut = null, bool? certifiant = null, string? tri = null)
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction("Index", "Dashboard");

        var parcours = await formationService.GetParcoursScoutsAsync([scout.Id]);
        parcours = ApplyParcoursFilters(parcours, q, statut, certifiant, tri);
        ViewBag.Recherche = q;
        ViewBag.Statut = statut;
        ViewBag.Certifiant = certifiant;
        ViewBag.Tri = tri;
        return View(parcours);
    }

    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> Suivre(Guid id)
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction(nameof(Catalogue));

        if (!await formationService.EstInscritAsync(id, scout.Id))
            return Forbid();

        var progression = await formationService.GetProgressionAsync(id, scout.Id);
        if (progression is null)
            return NotFound();

        return View(progression);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> MarquerLecon(Guid leconId, Guid formationId)
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction(nameof(Catalogue));

        if (!await formationService.EstInscritAsync(formationId, scout.Id))
            return Forbid();

        if (!await formationService.LeconAppartientFormationAsync(leconId, formationId))
            return Forbid();

        try
        {
            await formationService.MarquerLeconTermineeAsync(leconId, scout.Id);
        }
        catch (InvalidOperationException ex)
        {
            this.SetDomainError(ex);
        }

        return RedirectToAction(nameof(Suivre), new { id = formationId });
    }

    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> PasserQuiz(Guid quizId, Guid formationId)
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction(nameof(Catalogue));

        if (!await formationService.EstInscritAsync(formationId, scout.Id))
            return Forbid();

        if (!await formationService.QuizAppartientFormationAsync(quizId, formationId))
            return Forbid();

        var page = await formationService.GetQuizPassageAsync(quizId, formationId, scout.Id);
        if (page is null)
            return NotFound();

        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> SoumettreQuiz(Guid quizId, Guid formationId)
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction(nameof(Catalogue));

        if (!await formationService.EstInscritAsync(formationId, scout.Id))
            return Forbid();

        if (!await formationService.QuizAppartientFormationAsync(quizId, formationId))
            return Forbid();

        var reponses = new Dictionary<Guid, Guid>();
        foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("q_")))
        {
            var questionIdStr = key[2..];
            if (Guid.TryParse(questionIdStr, out var questionId) && Guid.TryParse(Request.Form[key], out var reponseId))
                reponses[questionId] = reponseId;
        }

        try
        {
            var tentative = await formationService.SoumettreQuizAsync(quizId, scout.Id, reponses);
            TempData[tentative.Reussi ? "Success" : "Error"] = tentative.Reussi
                ? $"Quiz reussi avec {tentative.Score}% !"
                : $"Score : {tentative.Score}%. Note minimale requise : {(await db.Quizzes.FindAsync(quizId))?.NoteMinimale}%.";
        }
        catch (InvalidOperationException ex)
        {
            this.SetDomainError(ex);
        }

        return RedirectToAction(nameof(Suivre), new { id = formationId });
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> FormationsEnfant(Guid scoutId, string? q = null, string? statut = null, bool? certifiant = null, string? tri = null)
    {
        var parent = await GetCurrentParentAsync();
        if (parent is null || !parent.Scouts.Any(s => s.Id == scoutId))
            return Forbid();

        var inscriptions = await formationService.GetParcoursScoutsAsync([scoutId]);
        inscriptions = ApplyParcoursFilters(inscriptions, q, statut, certifiant, tri);
        ViewBag.Scout = parent.Scouts.First(s => s.Id == scoutId);
        ViewBag.Recherche = q;
        ViewBag.Statut = statut;
        ViewBag.Certifiant = certifiant;
        ViewBag.Tri = tri;
        return View(inscriptions);
    }

    [Authorize(Roles = "Superviseur,Consultant")]
    public async Task<IActionResult> ConsulterCatalogue(string? q = null, string? niveau = null, string? session = null, bool? certifiant = null)
    {
        var formations = await formationService.GetCatalogueAsync(null, null);
        formations = ApplyCatalogueFilters(formations, q, niveau, session, certifiant);
        ViewBag.Recherche = q;
        ViewBag.Niveau = niveau;
        ViewBag.Session = session;
        ViewBag.Certifiant = certifiant;
        return View("Catalogue", formations);
    }

    [Authorize(Roles = "Superviseur,Consultant")]
    public async Task<IActionResult> ConsulterFormation(Guid id)
    {
        var formation = await formationService.GetDetailAsync(id);
        if (formation is null)
            return NotFound();

        ViewBag.Stats = await formationService.GetStatsAsync(id);
        ViewBag.LectureSeule = true;
        return View("Details", formation);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur")]
    public async Task<IActionResult> Statistiques()
    {
        var (page, ps) = ListPagination.Read(Request);
        var total = await formationService.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var formations = await formationService.GetPageAsync(skip, pageSize);
        var stats = await formationService.GetStatsByFormationAsync(formations.Select(f => f.Id));
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        ViewBag.Stats = stats;
        return View(formations);
    }

    private async Task ChargerViewBags(Guid? currentFormationId = null)
    {
        ViewBag.Branches = await db.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();

        ViewBag.FormationOptions = await db.Formations
            .AsNoTracking()
            .Where(f => f.Statut == StatutFormation.Publiee && (!currentFormationId.HasValue || f.Id != currentFormationId.Value))
            .OrderBy(f => f.Titre)
            .Select(f => new FormationDto
            {
                Id = f.Id,
                Titre = f.Titre
            })
            .ToListAsync();
    }

    private async Task<Scout?> GetCurrentScoutAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return null;

        return await db.Scouts.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
    }

    private async Task<Parent?> GetCurrentParentAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return null;

        return await db.Parents
            .Include(p => p.Scouts)
            .FirstOrDefaultAsync(p => p.Telephone == user.PhoneNumber);
    }

    private List<FormationDto> ApplyCatalogueFilters(
        List<FormationDto> formations,
        string? q,
        string? niveau,
        string? session,
        bool? certifiant)
    {
        IEnumerable<FormationDto> filtered = formations;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            filtered = filtered.Where(f =>
                f.Titre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(f.Description) && f.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(f.NomAuteur) && f.NomAuteur.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(niveau) && Enum.TryParse<NiveauFormation>(niveau, true, out var niveauValue))
            filtered = filtered.Where(f => f.Niveau == niveauValue);

        if (!string.IsNullOrWhiteSpace(session))
        {
            filtered = session.ToLowerInvariant() switch
            {
                "selfpaced" => filtered.Where(f => f.EstSessionSelfPaced),
                "upcoming" => filtered.Where(f => string.Equals(f.SessionStatut, "Bientot", StringComparison.OrdinalIgnoreCase)),
                "open" => filtered.Where(f => string.Equals(f.SessionStatut, "Session ouverte", StringComparison.OrdinalIgnoreCase)),
                _ => filtered
            };
        }

        if (certifiant == true)
            filtered = filtered.Where(f => f.DelivreBadge || f.DelivreAttestation || f.DelivreCertificat);

        return filtered.ToList();
    }

    private List<LmsParcoursItemDto> ApplyParcoursFilters(
        List<LmsParcoursItemDto> parcours,
        string? q,
        string? statut,
        bool? certifiant,
        string? tri)
    {
        IEnumerable<LmsParcoursItemDto> filtered = parcours;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            filtered = filtered.Where(p =>
                p.Titre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(p.Description) && p.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.ProchaineEtape) && p.ProchaineEtape.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(statut))
        {
            filtered = statut.ToLowerInvariant() switch
            {
                "encours" => filtered.Where(p => p.Statut == StatutInscription.EnCours),
                "terminee" => filtered.Where(p => p.Statut == StatutInscription.Terminee),
                "avenir" => filtered.Where(p => !p.EstSessionSelfPaced && p.DateOuvertureSession.HasValue && p.DateOuvertureSession.Value > DateTime.UtcNow),
                _ => filtered
            };
        }

        if (certifiant == true)
            filtered = filtered.Where(p => p.DelivreBadge || p.DelivreAttestation || p.DelivreCertificat);

        filtered = (tri ?? "recent").ToLowerInvariant() switch
        {
            "progression" => filtered.OrderByDescending(p => p.ProgressionPourcent).ThenBy(p => p.Titre),
            "titre" => filtered.OrderBy(p => p.Titre),
            "activite" => filtered.OrderByDescending(p => p.DerniereActiviteCours ?? DateTime.MinValue),
            _ => filtered.OrderByDescending(p => p.DateOuvertureSession ?? DateTime.MinValue).ThenBy(p => p.Titre)
        };

        return filtered.ToList();
    }

    private async Task<List<Guid>> GetEnrolledLearnerUserIdsAsync(Guid formationId, Guid? excludeUserId = null)
    {
        var users = await db.InscriptionsFormation
            .AsNoTracking()
            .Where(i => i.FormationId == formationId && i.Scout.UserId != null)
            .Select(i => i.Scout.UserId!.Value)
            .Distinct()
            .ToListAsync();

        if (!excludeUserId.HasValue)
            return users;

        return users.Where(id => id != excludeUserId.Value).ToList();
    }
}
