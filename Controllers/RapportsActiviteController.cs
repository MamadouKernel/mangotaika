using System.Net;
using System.Text;
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
            .Include(r => r.PiecesJointes.Where(p => !p.EstSupprime))
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
            .Include(r => r.PiecesJointes.Where(p => !p.EstSupprime))
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
            .Include(r => r.PiecesJointes.Where(p => !p.EstSupprime))
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
            .Include(r => r.PiecesJointes.Where(p => !p.EstSupprime))
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
                foreach (var piece in pieces)
                {
                    piece.EstSupprime = true;
                }
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

    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var rapport = await LoadRapportForExportAsync(id);
        if (rapport is null) return NotFound();
        if (!await CanAccessReportAsync(rapport.Activite.GroupeId)) return Forbid();

        var lines = BuildRapportLines(rapport);
        var titre = $"Rapport - {rapport.Activite.Titre}";
        var bytes = SimplePdfBuilder.BuildTextPdf(titre, lines);
        return File(bytes, "application/pdf", $"{BuildSafeFileName(rapport.Activite.Titre)}-rapport.pdf");
    }

    public async Task<IActionResult> ExportWord(Guid id)
    {
        var rapport = await LoadRapportForExportAsync(id);
        if (rapport is null) return NotFound();
        if (!await CanAccessReportAsync(rapport.Activite.GroupeId)) return Forbid();

        var html = BuildRapportWordHtml(rapport);
        return File(Encoding.UTF8.GetBytes(html), "application/msword", $"{BuildSafeFileName(rapport.Activite.Titre)}-rapport.doc");
    }

    private async Task<RapportActivite?> LoadRapportForExportAsync(Guid id)
    {
        return await db.RapportsActivite.AsNoTracking()
            .Include(r => r.Activite).ThenInclude(a => a.Groupe)
            .Include(r => r.Activite).ThenInclude(a => a.Participants.Where(p => !p.EstSupprime))
                .ThenInclude(p => p.Scout)
            .Include(r => r.Activite).ThenInclude(a => a.Participants.Where(p => !p.EstSupprime))
                .ThenInclude(p => p.Ressource)
            .Include(r => r.Createur)
            .Include(r => r.Valideur)
            .Include(r => r.PiecesJointes.Where(p => !p.EstSupprime))
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    private static List<string> BuildRapportLines(RapportActivite r)
    {
        var lines = new List<string>
        {
            "RAPPORT D'ACTIVITE",
            "==================",
            string.Empty,
            $"Activite : {r.Activite.Titre}",
            $"Groupe : {r.Activite.Groupe?.Nom ?? "District"}",
            $"Date debut : {r.Activite.DateDebut:dd/MM/yyyy}" + (r.Activite.DateFin.HasValue ? $" au {r.Activite.DateFin:dd/MM/yyyy}" : string.Empty),
            $"Lieu : {r.Activite.Lieu ?? "Non precise"}",
            $"Date de realisation : {r.DateRealisation:dd/MM/yyyy}",
            $"Nombre de participants : {r.NombreParticipants}",
            $"Statut : {r.Statut}",
            $"Cree par : {BuildPersonLabel(r.Createur)} le {r.DateCreation:dd/MM/yyyy}",
        };

        if (r.Valideur is not null && r.DateValidation.HasValue)
        {
            lines.Add($"Valide par : {BuildPersonLabel(r.Valideur)} le {r.DateValidation:dd/MM/yyyy}");
        }

        lines.Add(string.Empty);
        lines.Add("1. RESUME EXECUTIF");
        AppendBlock(lines, r.ResumeExecutif);
        lines.Add(string.Empty);
        lines.Add("2. RESULTATS OBTENUS");
        AppendBlock(lines, r.ResultatsObtenus);
        lines.Add(string.Empty);
        lines.Add("3. DIFFICULTES RENCONTREES");
        AppendBlock(lines, r.DifficultesRencontrees);
        lines.Add(string.Empty);
        lines.Add("4. RECOMMANDATIONS");
        AppendBlock(lines, r.Recommandations);

        if (!string.IsNullOrWhiteSpace(r.ObservationsComplementaires))
        {
            lines.Add(string.Empty);
            lines.Add("5. OBSERVATIONS COMPLEMENTAIRES");
            AppendBlock(lines, r.ObservationsComplementaires);
        }

        lines.Add(string.Empty);
        lines.Add($"LISTE DES PARTICIPANTS ({r.Activite.Participants.Count})");
        lines.Add("--------------------");

        var participants = r.Activite.Participants
            .OrderBy(p => p.Scout?.Nom ?? p.Ressource?.Nom ?? string.Empty)
            .ThenBy(p => p.Scout?.Prenom ?? p.Ressource?.Prenom ?? string.Empty)
            .ToList();

        if (participants.Count == 0)
        {
            lines.Add("Aucun participant enregistre.");
        }
        else
        {
            int idx = 1;
            foreach (var p in participants)
            {
                var label = p.Scout is not null
                    ? $"{p.Scout.Nom} {p.Scout.Prenom}".Trim() + (string.IsNullOrWhiteSpace(p.Scout.Matricule) ? string.Empty : $" ({p.Scout.Matricule})")
                    : p.Ressource is not null
                        ? $"{p.Ressource.Nom} {p.Ressource.Prenom}".Trim() + $" - {p.Ressource.Type}"
                        : "Participant inconnu";
                lines.Add($"{idx,3}. {label} - {p.Presence}");
                idx++;
            }
        }

        if (r.PiecesJointes.Any())
        {
            lines.Add(string.Empty);
            lines.Add("PIECES JOINTES");
            lines.Add("--------------");
            foreach (var piece in r.PiecesJointes)
            {
                lines.Add($"- {piece.NomFichier}");
            }
        }

        return lines;
    }

    private static void AppendBlock(List<string> lines, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            lines.Add("(Non renseigne)");
            return;
        }

        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            lines.Add(line.TrimEnd());
        }
    }

    private static string BuildPersonLabel(ApplicationUser? user)
        => user is null ? "Inconnu" : $"{user.Prenom} {user.Nom}".Trim();

    private static string BuildSafeFileName(string value)
    {
        var normalized = DatabaseText.NormalizeSearchKey(value).ToLowerInvariant();
        var chars = normalized.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var compact = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(compact) ? "rapport" : compact;
    }

    private static string BuildRapportWordHtml(RapportActivite r)
    {
        var titre = WebUtility.HtmlEncode(r.Activite.Titre);
        var groupe = WebUtility.HtmlEncode(r.Activite.Groupe?.Nom ?? "District");
        var lieu = WebUtility.HtmlEncode(r.Activite.Lieu ?? "Non precise");
        var dates = WebUtility.HtmlEncode($"{r.Activite.DateDebut:dd/MM/yyyy}" + (r.Activite.DateFin.HasValue ? $" au {r.Activite.DateFin:dd/MM/yyyy}" : string.Empty));
        var realisation = r.DateRealisation.ToString("dd/MM/yyyy");
        var statut = WebUtility.HtmlEncode(r.Statut.ToString());
        var createur = WebUtility.HtmlEncode(BuildPersonLabel(r.Createur));
        var dateCreation = r.DateCreation.ToString("dd/MM/yyyy");

        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Rapport - ").Append(titre).Append("</title>");
        sb.Append("<style>body{font-family:Arial,sans-serif;color:#1f2933;line-height:1.45;}.header{border-bottom:3px solid #597537;padding-bottom:14px;margin-bottom:24px;}.brand{color:#597537;font-size:18px;font-weight:700;letter-spacing:1px;}h1{color:#293a42;font-size:22px;margin:8px 0 0;}h2{color:#597537;font-size:14px;margin-top:18px;border-bottom:1px solid #d9e2d0;padding-bottom:4px;}p,div{font-size:12px;}table{width:100%;border-collapse:collapse;font-size:11px;margin-top:8px;}th,td{border:1px solid #cdd5dd;padding:6px 8px;text-align:left;}th{background:#eef3e6;color:#293a42;}.meta{margin:6px 0;}.meta strong{display:inline-block;min-width:160px;color:#293a42;}.footer{margin-top:28px;padding-top:12px;border-top:1px solid #d9e2d0;color:#667085;font-size:11px;}</style>");
        sb.Append("</head><body>");
        sb.Append("<div class=\"header\"><div class=\"brand\">MANGO TAIKA - DISTRICT SCOUT</div><h1>Rapport d'activite</h1></div>");

        sb.Append("<div class=\"meta\"><strong>Activite :</strong> ").Append(titre).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Groupe :</strong> ").Append(groupe).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Lieu :</strong> ").Append(lieu).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Dates :</strong> ").Append(dates).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Date de realisation :</strong> ").Append(realisation).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Participants :</strong> ").Append(r.NombreParticipants).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Statut :</strong> ").Append(statut).Append("</div>");
        sb.Append("<div class=\"meta\"><strong>Cree par :</strong> ").Append(createur).Append(" le ").Append(dateCreation).Append("</div>");
        if (r.Valideur is not null && r.DateValidation.HasValue)
        {
            sb.Append("<div class=\"meta\"><strong>Valide par :</strong> ").Append(WebUtility.HtmlEncode(BuildPersonLabel(r.Valideur))).Append(" le ").Append(r.DateValidation.Value.ToString("dd/MM/yyyy")).Append("</div>");
        }

        AppendWordSection(sb, "1. Resume executif", r.ResumeExecutif);
        AppendWordSection(sb, "2. Resultats obtenus", r.ResultatsObtenus);
        AppendWordSection(sb, "3. Difficultes rencontrees", r.DifficultesRencontrees);
        AppendWordSection(sb, "4. Recommandations", r.Recommandations);
        if (!string.IsNullOrWhiteSpace(r.ObservationsComplementaires))
        {
            AppendWordSection(sb, "5. Observations complementaires", r.ObservationsComplementaires);
        }

        sb.Append("<h2>Liste des participants (").Append(r.Activite.Participants.Count).Append(")</h2>");
        var participants = r.Activite.Participants
            .OrderBy(p => p.Scout?.Nom ?? p.Ressource?.Nom ?? string.Empty)
            .ThenBy(p => p.Scout?.Prenom ?? p.Ressource?.Prenom ?? string.Empty)
            .ToList();
        if (participants.Count == 0)
        {
            sb.Append("<p>Aucun participant enregistre.</p>");
        }
        else
        {
            sb.Append("<table><thead><tr><th>#</th><th>Nom et prenom</th><th>Matricule / Type</th><th>Categorie</th><th>Presence</th></tr></thead><tbody>");
            int idx = 1;
            foreach (var p in participants)
            {
                string nom; string complement; string categorie;
                if (p.Scout is not null)
                {
                    nom = WebUtility.HtmlEncode($"{p.Scout.Nom} {p.Scout.Prenom}".Trim());
                    complement = WebUtility.HtmlEncode(p.Scout.Matricule ?? "-");
                    categorie = "Scout";
                }
                else if (p.Ressource is not null)
                {
                    nom = WebUtility.HtmlEncode($"{p.Ressource.Nom} {p.Ressource.Prenom}".Trim());
                    complement = WebUtility.HtmlEncode(p.Ressource.Type.ToString());
                    categorie = "Ressource";
                }
                else
                {
                    nom = "Participant inconnu";
                    complement = "-";
                    categorie = "-";
                }

                sb.Append("<tr><td>").Append(idx).Append("</td><td>").Append(nom).Append("</td><td>").Append(complement).Append("</td><td>").Append(categorie).Append("</td><td>").Append(p.Presence).Append("</td></tr>");
                idx++;
            }
            sb.Append("</tbody></table>");
        }

        if (r.PiecesJointes.Any())
        {
            sb.Append("<h2>Pieces jointes</h2><ul>");
            foreach (var piece in r.PiecesJointes)
            {
                sb.Append("<li>").Append(WebUtility.HtmlEncode(piece.NomFichier)).Append("</li>");
            }
            sb.Append("</ul>");
        }

        sb.Append("<div class=\"footer\">Document genere automatiquement depuis le rapport d'activite.</div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void AppendWordSection(StringBuilder sb, string title, string? content)
    {
        sb.Append("<h2>").Append(WebUtility.HtmlEncode(title)).Append("</h2>");
        var text = string.IsNullOrWhiteSpace(content) ? "(Non renseigne)" : content;
        var encoded = WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br />");
        sb.Append("<div>").Append(encoded).Append("</div>");
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
