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

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create()
    {
        await LoadViewDataAsync();
        return View(new ActiviteCreateDto
        {
            DateDebut = DateTime.Now
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(ActiviteCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadViewDataAsync();
            return View(dto);
        }

        await activiteService.CreateAsync(dto, UserId);
        TempData["Success"] = "Activite creee avec succes.";
        return RedirectToAction(nameof(Index));
    }

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
        if (!ModelState.IsValid)
        {
            await LoadViewDataAsync();
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

        ViewBag.RapportActivite = await db.RapportsActivite
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ActiviteId == id);
        ViewBag.PresenceUrl = Url.Action(nameof(Presence), "Activites", new { id }, Request.Scheme) ?? string.Empty;

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

    public async Task<IActionResult> Presence(Guid id)
    {
        var activite = await activiteService.GetByIdAsync(id);
        if (activite is null) return NotFound();

        ViewBag.PresenceUrl = Url.Action(nameof(Presence), "Activites", new { id }, Request.Scheme) ?? string.Empty;
        return View(activite);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Brouillon) return NotFound();
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
        TempData["Success"] = "Activite soumise pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

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
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = "Activite validee.",
            TypeAction = "Validation"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activite validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

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
            Id = Guid.NewGuid(),
            ActiviteId = id,
            AuteurId = UserId,
            Contenu = $"Activite rejetee. Motif : {motif ?? "Non precise"}",
            TypeAction = "Rejet"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Activite rejetee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Demarrer(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.Validee) return NotFound();
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
        TempData["Success"] = "Activite en cours.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Terminer(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || a.Statut != StatutActivite.EnCours) return NotFound();
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
        TempData["Success"] = "Activite terminee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Archiver(Guid id)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null || (a.Statut != StatutActivite.Terminee && a.Statut != StatutActivite.Rejetee)) return NotFound();
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
        TempData["Success"] = "Activite archivee.";
        return RedirectToAction(nameof(Details), new { id });
    }

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
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterDocument(Guid id, IFormFile fichier, string? typeDocument)
    {
        var a = await db.Activites.FindAsync(id);
        if (a is null) return NotFound();
        if (fichier is null || fichier.Length == 0)
        {
            TempData["Error"] = "Veuillez selectionner un fichier.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var dir = Path.Combine(env.WebRootPath, "uploads", "activites", id.ToString());
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(fichier.FileName)}";
        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fichier.CopyToAsync(stream);
        }

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
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> SupprimerDocument(Guid id, Guid docId)
    {
        var doc = await db.DocumentsActivite.FindAsync(docId);
        if (doc is null) return NotFound();
        db.DocumentsActivite.Remove(doc);
        await db.SaveChangesAsync();
        TempData["Success"] = "Document supprime.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterParticipant(Guid id, Guid scoutId)
    {
        var exists = await db.ParticipantsActivite.AnyAsync(p => p.ActiviteId == id && p.ScoutId == scoutId);
        if (exists)
        {
            TempData["Error"] = "Ce scout est deja inscrit.";
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

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> RetirerParticipant(Guid id, Guid participantId)
    {
        var participant = await db.ParticipantsActivite.FindAsync(participantId);
        if (participant is not null)
        {
            db.ParticipantsActivite.Remove(participant);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Participant retire.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> MarquerPresence(Guid id, Guid participantId, StatutPresence presence)
    {
        var participant = await db.ParticipantsActivite.FindAsync(participantId);
        if (participant is not null)
        {
            participant.Presence = presence;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Presence mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> PresenceRapide(Guid id, Guid participantId, StatutPresence presence)
    {
        var participant = await db.ParticipantsActivite.FindAsync(participantId);
        if (participant is not null)
        {
            participant.Presence = presence;
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Presence mise a jour.";
        return RedirectToAction(nameof(Presence), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> AjouterCommentaire(Guid id, string contenu)
    {
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

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await activiteService.DeleteAsync(id);
        TempData["Success"] = "Activite supprimee.";
        return RedirectToAction(nameof(Index));
    }
}
