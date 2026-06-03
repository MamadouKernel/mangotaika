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

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant,EquipeDistrict,ChefGroupe,ChefUnite,Scout")]
public class ActivitesController(
    IActiviteService activiteService,
    IScoutQrService scoutQrService,
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IFileUploadService fileUploadService,
    INotificationDispatchService notificationDispatchService,
    OperationalAccessService accessService) : Controller
{
    private Guid UserId => Guid.Parse(userManager.GetUserId(User)!);

    private bool IsAdminOrManager => User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");

    private async Task<Guid?> GetChefGroupeScopeAsync()
    {
        if (IsAdminOrManager)
        {
            return null;
        }

        var currentUser = await accessService.GetCurrentUserAsync(User);
        if (currentUser?.GroupeId is Guid userGroupId)
        {
            return userGroupId;
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        return scout?.GroupeId;
    }

    private async Task LoadViewDataAsync(Guid? scopedGroupId = null)
    {
        var groupes = db.Groupes.Where(g => g.IsActive);
        if (scopedGroupId.HasValue)
        {
            groupes = groupes.Where(g => g.Id == scopedGroupId.Value);
        }

        ViewBag.Groupes = await groupes.OrderBy(g => g.Nom).ToListAsync();
        ViewBag.GroupeVerrouille = scopedGroupId.HasValue;
        ViewBag.GroupeSelectionne = scopedGroupId;
    }

    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var all = await activiteService.GetAllAsync();
        var scopedGroupId = await GetChefGroupeScopeAsync();
        if (scopedGroupId.HasValue)
        {
            all = all.Where(a => a.GroupeId == scopedGroupId.Value).ToList();
        }
        var total = all.Count;
        ViewBag.TotalActivites = total;
        ViewBag.CountsByStatut = all.GroupBy(a => a.Statut).ToDictionary(g => g.Key, g => g.Count());
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = all.Skip(skip).Take(pageSize).ToList();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Create()
    {
        var scopedGroupId = await GetChefGroupeScopeAsync();
        await LoadViewDataAsync(scopedGroupId);
        return View(new ActiviteCreateDto
        {
            DateDebut = DateTime.Now,
            GroupeId = scopedGroupId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Create(ActiviteCreateDto dto)
    {
        var scopedGroupId = await GetChefGroupeScopeAsync();
        if (scopedGroupId.HasValue)
        {
            dto.GroupeId = scopedGroupId;
        }

        if (!ModelState.IsValid)
        {
            await LoadViewDataAsync(scopedGroupId);
            return View(dto);
        }

        await activiteService.CreateAsync(dto, UserId);
        TempData["Success"] = "Activite creee avec succes.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var activite = await activiteService.GetByIdAsync(id);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();
        await LoadViewDataAsync(await GetChefGroupeScopeAsync());
        return View(activite);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Edit(Guid id, ActiviteCreateDto dto)
    {
        var existing = await db.Activites.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (existing is null) return NotFound();
        if (!await CanManageActivityAsync(existing.GroupeId)) return Forbid();
        var scopedGroupId = await GetChefGroupeScopeAsync();
        if (scopedGroupId.HasValue)
        {
            dto.GroupeId = scopedGroupId;
        }

        if (!ModelState.IsValid)
        {
            await LoadViewDataAsync(scopedGroupId);
            return View(dto);
        }

        var result = await activiteService.UpdateAsync(id, dto);
        if (!result) return NotFound();
        TempData["Success"] = "Activite mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var activite = await activiteService.GetByIdAsync(id);
        if (activite is null) return NotFound();
        if (!await CanViewActivityAsync(activite.GroupeId)) return Forbid();

        ViewBag.RapportActivite = await db.RapportsActivite
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ActiviteId == id);
        ViewBag.PresenceUrl = Url.Action(nameof(Presence), "Activites", new { id }, Request.Scheme) ?? string.Empty;
        ViewBag.CanManageActivity = await CanManageActivityAsync(activite.GroupeId);

        if (activite.GroupeId.HasValue)
        {
            ViewBag.ScoutsGroupe = await db.Scouts
                .Include(s => s.Branche)
                .Where(s => s.GroupeId == activite.GroupeId && s.IsActive)
                .OrderBy(s => s.Nom)
                .ToListAsync();
        }
        else
        {
            ViewBag.ScoutsGroupe = await db.Scouts
                .Include(s => s.Branche)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Nom)
                .ToListAsync();
        }

        ViewBag.Ressources = await db.Ressources
            .Where(r => r.IsActive && (!activite.GroupeId.HasValue || r.GroupeId == activite.GroupeId))
            .OrderBy(r => r.Nom)
            .ThenBy(r => r.Prenom)
            .ToListAsync();

        return View(activite);
    }

    public async Task<IActionResult> Presence(Guid id)
    {
        var activite = await activiteService.GetByIdAsync(id);
        if (activite is null) return NotFound();
        if (!await CanViewActivityAsync(activite.GroupeId)) return Forbid();

        ViewBag.PresenceUrl = Url.Action(nameof(Presence), "Activites", new { id }, Request.Scheme) ?? string.Empty;
        return View(activite);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Brouillon) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        a.Statut = StatutActivite.Soumise;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite soumise pour validation.",
            TypeAction = "Soumission"
        });
        await db.SaveChangesAsync();
        await NotifyActivityStakeholdersAsync(a, "Activite soumise", $"L'activite \"{a.Titre}\" a ete soumise pour validation.");
        TempData["Success"] = "Activite soumise pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Valider(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Soumise) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        a.Statut = StatutActivite.Validee;
        a.MotifRejet = null;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite validee.",
            TypeAction = "Validation"
        });
        await db.SaveChangesAsync();
        await NotifyActivityStakeholdersAsync(a, "Activite validee", $"L'activite \"{a.Titre}\" a ete validee.");
        TempData["Success"] = "Activite validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Rejeter(Guid id, string? motif)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Soumise) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        a.Statut = StatutActivite.Rejetee;
        a.MotifRejet = motif;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = $"Activite rejetee. Motif : {motif ?? "Non precise"}",
            TypeAction = "Rejet"
        });
        await db.SaveChangesAsync();
        await NotifyActivityStakeholdersAsync(a, "Activite rejetee", $"L'activite \"{a.Titre}\" a ete rejetee. Motif : {motif ?? "Non precise"}");
        TempData["Success"] = "Activite rejetee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Demarrer(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Validee) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        a.Statut = StatutActivite.EnCours;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite demarree.",
            TypeAction = "Demarrage"
        });
        await db.SaveChangesAsync();
        await NotifyActivityStakeholdersAsync(a, "Activite demarree", $"L'activite \"{a.Titre}\" a demarre.");
        TempData["Success"] = "Activite en cours.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Terminer(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.EnCours) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        a.Statut = StatutActivite.Terminee;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite terminee.",
            TypeAction = "Cloture"
        });
        await db.SaveChangesAsync();
        await NotifyActivityStakeholdersAsync(a, "Activite terminee", $"L'activite \"{a.Titre}\" est terminee.");
        TempData["Success"] = "Activite terminee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Archiver(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || (a.Statut != StatutActivite.Terminee && a.Statut != StatutActivite.Rejetee)) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();

        if (a.Statut == StatutActivite.Terminee && !a.DateCloturePointage.HasValue)
        {
            a.DateCloturePointage = DateTime.UtcNow;
            db.CommentairesActivite.Add(new CommentaireActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                AuteurId = UserId,
                Contenu = "Pointage cloture automatiquement lors de l'archivage de l'activite.",
                TypeAction = "Pointage"
            });
        }
        a.Statut = StatutActivite.Archivee;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite archivee.",
            TypeAction = "Archivage"
        });
        await db.SaveChangesAsync();
        await NotifyActivityStakeholdersAsync(a, "Activite archivee", $"L'activite \"{a.Titre}\" a ete archivee.");
        TempData["Success"] = "Activite archivee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Rebrouillon(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Rejetee) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        a.Statut = StatutActivite.Brouillon;
        a.MotifRejet = null;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite remise en brouillon pour correction.",
            TypeAction = "Correction"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activite remise en brouillon.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> AjouterDocument(Guid id, IFormFile fichier, string? typeDocument)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null) return NotFound();
        if (!await CanManageActivityAsync(a.GroupeId)) return Forbid();
        if (fichier is null || fichier.Length == 0)
        {
            TempData["Error"] = "Veuillez selectionner un fichier.";
            return RedirectToAction(nameof(Details), new { id });
        }

        string cheminFichier;
        try
        {
            cheminFichier = await fileUploadService.SaveDocumentAsync(
                fichier,
                Path.Combine("activites", id.ToString()),
                [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".jpg", ".jpeg", ".png", ".webp", ".gif"]);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        db.DocumentsActivite.Add(new DocumentActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            NomFichier = fichier.FileName,
            CheminFichier = cheminFichier,
            TypeDocument = typeDocument
        });

        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = $"Document ajoute : {fichier.FileName}",
            TypeAction = "Document"
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Document ajoute.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> SupprimerDocument(Guid id, Guid docId)
    {
        var activite = await db.Activites.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();
        var doc = await db.DocumentsActivite.FirstOrDefaultAsync(d => d.Id == docId && d.ActiviteId == id && !d.EstSupprime);
        if (doc is null) return NotFound();
        doc.EstSupprime = true;
        await db.SaveChangesAsync();
        TempData["Success"] = "Document supprime.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> AjouterParticipant(Guid id, Guid scoutId)
        => await AjouterParticipants(id, scoutId == Guid.Empty ? [] : [scoutId], null, null);

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> AjouterParticipants(Guid id, List<Guid>? scoutIds, List<Guid>? ressourceIds, string? matricules)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (activite.DateCloturePointage.HasValue)
        {
            TempData["Warning"] = "Le pointage est cloture. Reouvrez-le avant de modifier la liste des participants.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var selectedScoutIds = scoutIds?
            .Where(scoutId => scoutId != Guid.Empty)
            .Distinct()
            .ToList()
            ?? [];
        var importedMatricules = ParseMatricules(matricules);

        var importedScouts = importedMatricules.Count == 0
            ? []
            : await db.Scouts
                .Where(s => s.IsActive && s.Matricule != null && importedMatricules.Contains(s.Matricule.ToUpper()))
                .Select(s => new { s.Id, s.Matricule })
                .ToListAsync();

        var allScoutIds = selectedScoutIds
            .Concat(importedScouts.Select(s => s.Id))
            .Distinct()
            .ToList();

        if (allScoutIds.Count == 0)
        {
            TempData["Error"] = "Selectionnez au moins un scout ou renseignez une liste de matricules.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var existingScoutIds = await db.ParticipantsActivite
            .Where(p => p.ActiviteId == id && !p.EstSupprime && p.ScoutId.HasValue && allScoutIds.Contains(p.ScoutId.Value))
            .Select(p => p.ScoutId!.Value)
            .ToListAsync();
        var existingScoutSet = existingScoutIds.ToHashSet();

        var scoutsToAdd = await db.Scouts
            .Where(s => s.IsActive && allScoutIds.Contains(s.Id) && !existingScoutSet.Contains(s.Id))
            .OrderBy(s => s.Nom)
            .ThenBy(s => s.Prenom)
            .ToListAsync();

        if (scoutsToAdd.Count > 0)
        {
            db.ParticipantsActivite.AddRange(scoutsToAdd.Select(scout => new ParticipantActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                ScoutId = scout.Id
            }));

            db.CommentairesActivite.Add(new CommentaireActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                AuteurId = UserId,
                Contenu = $"{scoutsToAdd.Count} participant(s) ajoute(s) a la liste.",
                TypeAction = "Participants"
            });

            await db.SaveChangesAsync();
        }

        var selectedRessourceIds = ressourceIds?
            .Where(ressourceId => ressourceId != Guid.Empty)
            .Distinct()
            .ToList()
            ?? [];

        var existingRessourceIds = selectedRessourceIds.Count == 0
            ? []
            : await db.ParticipantsActivite
                .Where(p => p.ActiviteId == id && !p.EstSupprime && p.RessourceId.HasValue && selectedRessourceIds.Contains(p.RessourceId.Value))
                .Select(p => p.RessourceId!.Value)
                .ToListAsync();

        var ressourcesToAdd = selectedRessourceIds.Count == 0
            ? []
            : await db.Ressources
                .Where(r => r.IsActive
                    && selectedRessourceIds.Contains(r.Id)
                    && !existingRessourceIds.Contains(r.Id)
                    && (!activite.GroupeId.HasValue || r.GroupeId == activite.GroupeId))
                .ToListAsync();

        if (ressourcesToAdd.Count > 0)
        {
            db.ParticipantsActivite.AddRange(ressourcesToAdd.Select(ressource => new ParticipantActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                RessourceId = ressource.Id
            }));

            db.CommentairesActivite.Add(new CommentaireActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                AuteurId = UserId,
                Contenu = $"{ressourcesToAdd.Count} ressource(s) ajoutee(s) a la liste.",
                TypeAction = "Participants"
            });

            await db.SaveChangesAsync();
        }

        var foundImportedMatricules = importedScouts
            .Select(s => NormalizeMatricule(s.Matricule))
            .Where(matricule => !string.IsNullOrWhiteSpace(matricule))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var notFoundCount = importedMatricules.Count(matricule => !foundImportedMatricules.Contains(matricule));
        var duplicateCount = existingScoutIds.Count;

        var importSummary = new List<string>();
        if (duplicateCount > 0)
        {
            importSummary.Add($"{duplicateCount} deja inscrit(s)");
        }

        if (notFoundCount > 0)
        {
            importSummary.Add($"{notFoundCount} matricule(s) introuvable(s)");
        }

        var summarySuffix = importSummary.Count > 0
            ? $" ({string.Join(", ", importSummary)})."
            : ".";

        var totalAdded = scoutsToAdd.Count + ressourcesToAdd.Count;
        if (totalAdded > 0)
        {
            TempData["Success"] = $"{totalAdded} participant(s) ajoute(s){summarySuffix}";
        }
        else
        {
            TempData["Error"] = $"Aucun nouveau participant ajoute{summarySuffix}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> RetirerParticipant(Guid id, Guid participantId)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (activite.DateCloturePointage.HasValue)
        {
            TempData["Warning"] = "Le pointage est cloture. Reouvrez-le avant de retirer un participant.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var participant = await db.ParticipantsActivite.FirstOrDefaultAsync(p => p.Id == participantId && p.ActiviteId == id && !p.EstSupprime);
        if (participant is not null)
        {
            participant.EstSupprime = true;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Participant retire.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> MarquerPresence(Guid id, Guid participantId, StatutPresence presence)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (!PointageEstAccessible(activite))
        {
            TempData["Warning"] = "Le pointage n'est disponible que pour une activite en cours ou terminee.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (activite.DateCloturePointage.HasValue)
        {
            TempData["Warning"] = "Le pointage est cloture. Reouvrez-le avant de modifier une presence.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var participant = await db.ParticipantsActivite.FirstOrDefaultAsync(p => p.Id == participantId && p.ActiviteId == id && !p.EstSupprime);
        if (participant is not null)
        {
            participant.Presence = presence;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Presence mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> PresenceRapide(Guid id, Guid participantId, StatutPresence presence)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (!PointageEstAccessible(activite))
        {
            TempData["Warning"] = "Le pointage n'est disponible que pour une activite en cours ou terminee.";
            return RedirectToAction(nameof(Presence), new { id });
        }

        if (activite.DateCloturePointage.HasValue)
        {
            TempData["Warning"] = "Le pointage est cloture. Reouvrez-le avant de modifier une presence.";
            return RedirectToAction(nameof(Presence), new { id });
        }

        var participant = await db.ParticipantsActivite.FirstOrDefaultAsync(p => p.Id == participantId && p.ActiviteId == id && !p.EstSupprime);
        if (participant is not null)
        {
            participant.Presence = presence;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Presence mise a jour.";
        return RedirectToAction(nameof(Presence), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> CloturerPointage(Guid id, string? returnAction)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (!PointageEstAccessible(activite))
        {
            TempData["Warning"] = "Le pointage ne peut etre cloture qu'une fois l'activite en cours ou terminee.";
            return RedirectToActivityPage(id, returnAction);
        }

        if (activite.DateCloturePointage.HasValue)
        {
            TempData["Warning"] = "Le pointage est deja cloture.";
            return RedirectToActivityPage(id, returnAction);
        }

        activite.DateCloturePointage = DateTime.UtcNow;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Pointage cloture. Les presences sont maintenant verrouillees.",
            TypeAction = "Pointage"
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Pointage cloture. Les presences sont verrouillees jusqu'a reouverture.";
        return RedirectToActivityPage(id, returnAction);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> ReouvrirPointage(Guid id, string? returnAction)
    {
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (!activite.DateCloturePointage.HasValue)
        {
            TempData["Warning"] = "Le pointage est deja ouvert.";
            return RedirectToActivityPage(id, returnAction);
        }

        if (activite.Statut == StatutActivite.Archivee)
        {
            TempData["Warning"] = "Une activite archivee ne peut plus reouvrir son pointage.";
            return RedirectToActivityPage(id, returnAction);
        }

        activite.DateCloturePointage = null;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Pointage reouvert pour correction des presences.",
            TypeAction = "Pointage"
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Pointage reouvert. Les presences peuvent de nouveau etre ajustees.";
        return RedirectToActivityPage(id, returnAction);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> AjouterCommentaire(Guid id, string contenu)
    {
        var activite = await db.Activites.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null) return NotFound();
        if (!await CanManageActivityAsync(activite.GroupeId)) return Forbid();

        if (string.IsNullOrWhiteSpace(contenu))
        {
            TempData["Error"] = "Le commentaire ne peut pas etre vide.";
            return RedirectToAction(nameof(Details), new { id });
        }

        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = contenu,
            TypeAction = "Commentaire"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Commentaire ajoute.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> ScannerScoutQr(Guid id, [FromBody] PresenceScoutScanRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ScannedCode))
        {
            return BadRequest(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Scannez un QR scout ou saisissez un matricule valide."
            });
        }

        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null)
        {
            return NotFound(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Activite introuvable ou supprimee. Actualisez la page puis reessayez."
            });
        }
        if (!await CanManageActivityAsync(activite.GroupeId))
        {
            return Forbid();
        }

        if (!PointageEstAccessible(activite))
        {
            return Conflict(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Le pointage n'est disponible que pour une activite en cours ou terminee."
            });
        }

        if (activite.DateCloturePointage.HasValue)
        {
            return Conflict(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Le pointage est cloture. Reouvrez-le avant de scanner un scout."
            });
        }

        var (scout, errorMessage) = await ResolveScoutFromScannedCodeAsync(request.ScannedCode);
        if (scout is null)
        {
            return BadRequest(new PresenceScoutScanResponse
            {
                Success = false,
                Message = errorMessage ?? "Scout introuvable. Verifiez le matricule ou scannez le QR officiel du scout."
            });
        }

        var participant = await db.ParticipantsActivite
            .Include(p => p.Scout)
            .FirstOrDefaultAsync(p => p.ActiviteId == id && p.ScoutId == scout.Id && !p.EstSupprime);

        if (participant is null)
        {
            return Conflict(new PresenceScoutScanResponse
            {
                Success = false,
                CanAddParticipant = true,
                ScoutId = scout.Id,
                ScoutName = $"{scout.Prenom} {scout.Nom}",
                Matricule = scout.Matricule,
                Message = $"{scout.Prenom} {scout.Nom} n'est pas inscrit a cette activite. Vous pouvez l'ajouter puis le marquer present."
            });
        }

        var previousPresence = participant.Presence;
        participant.Presence = StatutPresence.Present;
        await db.SaveChangesAsync();

        var counts = await db.ParticipantsActivite
            .Where(p => p.ActiviteId == id && !p.EstSupprime)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Presents = g.Count(p => p.Presence == StatutPresence.Present),
                Absents = g.Count(p => p.Presence == StatutPresence.Absent),
                Excuses = g.Count(p => p.Presence == StatutPresence.Excuse),
                Pending = g.Count(p => p.Presence == StatutPresence.Inscrit)
            })
            .FirstAsync();

        var message = previousPresence == StatutPresence.Present
            ? $"{scout.Prenom} {scout.Nom} etait deja marque present."
            : $"{scout.Prenom} {scout.Nom} a ete marque present.";

        return Json(new PresenceScoutScanResponse
        {
            Success = true,
            Message = message,
            ParticipantId = participant.Id,
            ScoutId = scout.Id,
            ScoutName = $"{scout.Prenom} {scout.Nom}",
            Matricule = scout.Matricule,
            PreviousPresence = previousPresence.ToString(),
            CurrentPresence = StatutPresence.Present.ToString(),
            Presents = counts.Presents,
            Absents = counts.Absents,
            Excuses = counts.Excuses,
            Pending = counts.Pending
        });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe,Scout")]
    public async Task<IActionResult> AjouterParticipantEtMarquerPresent(Guid id, [FromBody] PresenceScoutAddRequest request)
    {
        if (request is null || request.ScoutId == Guid.Empty)
        {
            return BadRequest(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Le scout a ajouter n'est pas valide. Selectionnez un scout depuis la liste proposee ou scannez son QR officiel."
            });
        }

        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (activite is null)
        {
            return NotFound(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Activite introuvable ou supprimee. Actualisez la page puis reessayez."
            });
        }

        if (!PointageEstAccessible(activite))
        {
            return Conflict(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Le pointage n'est disponible que pour une activite en cours ou terminee."
            });
        }

        if (activite.DateCloturePointage.HasValue)
        {
            return Conflict(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Le pointage est cloture. Reouvrez-le avant d'ajouter un participant tardif."
            });
        }

        var scout = await db.Scouts.FirstOrDefaultAsync(s => s.Id == request.ScoutId && s.IsActive);
        if (scout is null)
        {
            return NotFound(new PresenceScoutScanResponse
            {
                Success = false,
                Message = "Scout introuvable ou inactif. Verifiez que la fiche scout existe et qu'elle est active."
            });
        }

        var participant = await db.ParticipantsActivite
            .Include(p => p.Scout)
            .FirstOrDefaultAsync(p => p.ActiviteId == id && p.ScoutId == scout.Id && !p.EstSupprime);

        var previousPresence = StatutPresence.Inscrit;
        if (participant is null)
        {
            participant = new ParticipantActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                ScoutId = scout.Id,
                Presence = StatutPresence.Present
            };
            db.ParticipantsActivite.Add(participant);
            db.CommentairesActivite.Add(new CommentaireActivite
            {
                Id = Guid.NewGuid(),
                ActiviteId = id,
                AuteurId = UserId,
                Contenu = $"Participant tardif ajoute puis marque present : {scout.Prenom} {scout.Nom}.",
                TypeAction = "Pointage"
            });
        }
        else
        {
            previousPresence = participant.Presence;
            participant.Presence = StatutPresence.Present;
        }

        await db.SaveChangesAsync();

        var counts = await db.ParticipantsActivite
            .Where(p => p.ActiviteId == id && !p.EstSupprime)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Presents = g.Count(p => p.Presence == StatutPresence.Present),
                Absents = g.Count(p => p.Presence == StatutPresence.Absent),
                Excuses = g.Count(p => p.Presence == StatutPresence.Excuse),
                Pending = g.Count(p => p.Presence == StatutPresence.Inscrit)
            })
            .FirstAsync();

        return Json(new PresenceScoutScanResponse
        {
            Success = true,
            Message = $"{scout.Prenom} {scout.Nom} a ete ajoute a l'activite puis marque present.",
            ParticipantId = participant.Id,
            ScoutId = scout.Id,
            ScoutName = $"{scout.Prenom} {scout.Nom}",
            Matricule = scout.Matricule,
            PreviousPresence = previousPresence.ToString(),
            CurrentPresence = StatutPresence.Present.ToString(),
            Presents = counts.Presents,
            Absents = counts.Absents,
            Excuses = counts.Excuses,
            Pending = counts.Pending
        });
    }

    private static bool PointageEstAccessible(Activite activite)
    {
        return activite.Statut == StatutActivite.EnCours || activite.Statut == StatutActivite.Terminee;
    }

    private async Task<bool> CanViewActivityAsync(Guid? groupeId)
    {
        if (IsAdminOrManager || accessService.IsSupervision(User))
        {
            return true;
        }
        var scopedGroupId = await GetChefGroupeScopeAsync();
        return scopedGroupId.HasValue && groupeId == scopedGroupId.Value;
    }

    private async Task<bool> CanManageActivityAsync(Guid? groupeId)
    {
        if (IsAdminOrManager)
        {
            return true;
        }

        var scout = await accessService.GetCurrentScoutAsync(User);
        if (scout is not null && IsChefGroupeFunction(scout.Fonction))
        {
            return scout.GroupeId == groupeId;
        }

        if (!User.IsInRole("ChefGroupe"))
        {
            return false;
        }

        var scopedGroupId = await GetChefGroupeScopeAsync();
        return scopedGroupId.HasValue && groupeId == scopedGroupId.Value;
    }

    private static bool IsChefGroupeFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction);
        return normalizedFunction.Contains(DatabaseText.NormalizeSearchKey("CHEF DE GROUPE"), StringComparison.Ordinal)
            || normalizedFunction.Contains(DatabaseText.NormalizeSearchKey("CHEF GROUPE"), StringComparison.Ordinal)
            || normalizedFunction == "CG"
            || normalizedFunction.StartsWith("CG", StringComparison.Ordinal);
    }

    private async Task NotifyActivityStakeholdersAsync(Activite activite, string title, string message)
    {
        var recipients = await db.Users
            .Where(u => u.Id == activite.CreateurId || (activite.GroupeId.HasValue && u.GroupeId == activite.GroupeId))
            .Select(u => u.Id)
            .ToListAsync();

        var adminRoleIds = await db.Roles
            .Where(r => r.Name == "Administrateur" || r.Name == "Gestionnaire")
            .Select(r => r.Id)
            .ToListAsync();

        var adminIds = await db.UserRoles
            .Where(ur => adminRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .ToListAsync();

        await notificationDispatchService.SendAsync(
            recipients.Concat(adminIds),
            title,
            message,
            "Activites",
            $"/Activites/Details/{activite.Id}");
    }

    private static List<string> ParseMatricules(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        return rawValue
            .Split(['\r', '\n', ',', ';', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeMatricule)
            .Where(matricule => !string.IsNullOrWhiteSpace(matricule))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeMatricule(string? matricule)
        => string.IsNullOrWhiteSpace(matricule) ? string.Empty : matricule.Trim().ToUpperInvariant();

    private IActionResult RedirectToActivityPage(Guid id, string? returnAction)
    {
        return string.Equals(returnAction, nameof(Details), StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Presence), new { id });
    }

    private async Task<(Scout? Scout, string? ErrorMessage)> ResolveScoutFromScannedCodeAsync(string scannedCode)
    {
        var rawValue = scannedCode.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return (null, "Scannez un QR scout ou saisissez un matricule valide.");
        }

        if (rawValue.StartsWith("MTSCOUT:", StringComparison.OrdinalIgnoreCase))
        {
            if (!scoutQrService.TryReadScoutId(rawValue, out var scoutId))
            {
                return (null, "Le QR scout scanne est invalide ou corrompu. Regenerez le QR depuis la fiche scout puis reessayez.");
            }

            var scoutByQr = await db.Scouts.FirstOrDefaultAsync(s => s.Id == scoutId && s.IsActive);
            return scoutByQr is null
                ? (null, "Le QR scout ne correspond a aucun profil actif. Verifiez que le scout existe et que sa fiche est active.")
                : (scoutByQr, null);
        }

        var normalizedMatricule = rawValue.ToUpperInvariant();
        var scoutByMatricule = await db.Scouts.FirstOrDefaultAsync(s => s.IsActive && s.Matricule != null && s.Matricule.ToUpper() == normalizedMatricule);
        return scoutByMatricule is null
            ? (null, "Aucun scout actif ne correspond a ce matricule ou code. Verifiez le format du matricule ou demandez une mise a jour de la fiche scout.")
            : (scoutByMatricule, null);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,ChefGroupe")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!IsAdminOrManager) return Forbid();
        await activiteService.DeleteAsync(id);
        TempData["Success"] = "Activite supprimee.";
        return RedirectToAction(nameof(Index));
    }
}

