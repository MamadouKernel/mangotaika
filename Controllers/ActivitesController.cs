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

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class ActivitesController(
    IActiviteService activiteService,
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IWebHostEnvironment env) : Controller
{
    private Guid UserId => Guid.Parse(userManager.GetUserId(User)!);

    private async Task LoadViewDataAsync()
    {
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
    }

    // === INDEX ===
    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var all = await activiteService.GetAllAsync();
        var total = all.Count;
        ViewBag.TotalActivites = total;
        ViewBag.CountsByStatut = all.GroupBy(a => a.Statut).ToDictionary(g => g.Key, g => g.Count());
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var pageItems = all.Skip(skip).Take(pageSize).ToList();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(pageItems);
    }

    // === CREATE ===
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadViewDataAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(ActiviteCreateDto dto)
    {
        if (!ModelState.IsValid) { await LoadViewDataAsync(); return View(dto); }
        await activiteService.CreateAsync(dto, UserId);
        TempData["Success"] = "Activité créée avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // === EDIT ===
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var activite = await activiteService.GetByIdAsync(id);
        if (activite is null) return NotFound();
        await LoadViewDataAsync();
        return View(activite);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id, ActiviteCreateDto dto)
    {
        if (!ModelState.IsValid) { await LoadViewDataAsync(); return View(dto); }
        var result = await activiteService.UpdateAsync(id, dto);
        if (!result) return NotFound();
        TempData["Success"] = "Activité mise à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === DETAILS ===
    public async Task<IActionResult> Details(Guid id)
    {
        var activite = await activiteService.GetByIdAsync(id);
        if (activite is null) return NotFound();
        // Charger les scouts du groupe pour l'ajout de participants
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
        return View(activite);
    }

    // === WORKFLOW : Soumettre ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Brouillon) return NotFound();
        a.Statut = StatutActivite.Soumise;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = "Activité soumise pour validation.", TypeAction = "Soumission"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité soumise pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === WORKFLOW : Valider ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Valider(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Soumise) return NotFound();
        a.Statut = StatutActivite.Validee;
        a.MotifRejet = null;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = "Activité validée.", TypeAction = "Validation"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité validée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === WORKFLOW : Rejeter ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Rejeter(Guid id, string? motif)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Soumise) return NotFound();
        a.Statut = StatutActivite.Rejetee;
        a.MotifRejet = motif;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = $"Activité rejetée. Motif : {motif ?? "Non précisé"}", TypeAction = "Rejet"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité rejetée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === WORKFLOW : Démarrer (En cours) ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Demarrer(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Validee) return NotFound();
        a.Statut = StatutActivite.EnCours;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = "Activité démarrée.", TypeAction = "Démarrage"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité en cours.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === WORKFLOW : Terminer ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Terminer(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.EnCours) return NotFound();
        a.Statut = StatutActivite.Terminee;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = "Activité terminée.", TypeAction = "Clôture"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité terminée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === WORKFLOW : Archiver ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Archiver(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || (a.Statut != StatutActivite.Terminee && a.Statut != StatutActivite.Rejetee)) return NotFound();
        a.Statut = StatutActivite.Archivee;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = "Activité archivée.", TypeAction = "Archivage"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité archivée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === WORKFLOW : Remettre en brouillon (après rejet) ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Rebrouillon(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Rejetee) return NotFound();
        a.Statut = StatutActivite.Brouillon;
        a.MotifRejet = null;
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = "Activité remise en brouillon pour correction.", TypeAction = "Correction"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activité remise en brouillon.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === DOCUMENTS : Upload ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterDocument(Guid id, IFormFile fichier, string? typeDocument)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null) return NotFound();
        if (fichier is null || fichier.Length == 0)
        {
            TempData["Error"] = "Veuillez sélectionner un fichier.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var dir = Path.Combine(env.WebRootPath, "uploads", "activites", id.ToString());
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(fichier.FileName)}";
        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await fichier.CopyToAsync(stream);

        db.DocumentsActivite.Add(new DocumentActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            NomFichier = fichier.FileName,
            CheminFichier = $"/uploads/activites/{id}/{fileName}",
            TypeDocument = typeDocument
        });

        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = $"Document ajouté : {fichier.FileName}", TypeAction = "Document"
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Document ajouté.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === DOCUMENTS : Supprimer ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> SupprimerDocument(Guid id, Guid docId)
    {
        var doc = await db.DocumentsActivite.FindAsync(docId);
        if (doc is null) return NotFound();
        db.DocumentsActivite.Remove(doc);
        await db.SaveChangesAsync();
        TempData["Success"] = "Document supprimé.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === PARTICIPANTS : Ajouter ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterParticipant(Guid id, Guid scoutId)
    {
        var exists = await db.ParticipantsActivite.AnyAsync(p => p.ActiviteId == id && p.ScoutId == scoutId);
        if (exists)
        {
            TempData["Error"] = "Ce scout est déjà inscrit.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var scout = await db.Scouts.FindAsync(scoutId);
        if (scout is null) return NotFound();

        db.ParticipantsActivite.Add(new ParticipantActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = id,
            ScoutId = scoutId
        });
        await db.SaveChangesAsync();
        TempData["Success"] = $"{scout.Prenom} {scout.Nom} inscrit.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === PARTICIPANTS : Retirer ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> RetirerParticipant(Guid id, Guid participantId)
    {
        var p = await db.ParticipantsActivite.FindAsync(participantId);
        if (p is not null) db.ParticipantsActivite.Remove(p);
        await db.SaveChangesAsync();
        TempData["Success"] = "Participant retiré.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === PARTICIPANTS : Marquer présence ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> MarquerPresence(Guid id, Guid participantId, StatutPresence presence)
    {
        var p = await db.ParticipantsActivite.FindAsync(participantId);
        if (p is not null) { p.Presence = presence; await db.SaveChangesAsync(); }
        TempData["Success"] = "Présence mise à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === COMMENTAIRE ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterCommentaire(Guid id, string contenu)
    {
        if (string.IsNullOrWhiteSpace(contenu))
        {
            TempData["Error"] = "Le commentaire ne peut pas être vide.";
            return RedirectToAction(nameof(Details), new { id });
        }
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(), ActiviteId = id, AuteurId = UserId,
            Contenu = contenu, TypeAction = "Commentaire"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Commentaire ajouté.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // === DELETE ===
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await activiteService.DeleteAsync(id);
        TempData["Success"] = "Activité supprimée.";
        return RedirectToAction(nameof(Index));
    }
}
