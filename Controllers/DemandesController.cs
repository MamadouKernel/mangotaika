using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MangoTaika.Hubs;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize]
public class DemandesController(AppDbContext db, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> hub) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var isSupervision = User.IsInRole("Superviseur") || User.IsInRole("Consultant");

        var query = db.DemandesAutorisation
            .Include(d => d.Demandeur)
            .Include(d => d.Groupe)
            .AsQueryable();

        if (!isAdmin && !isSupervision)
            query = query.Where(d => d.DemandeurId == userId);

        var (page, ps) = ListPagination.Read(Request);
        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var demandes = await query.OrderByDescending(d => d.DateCreation).Skip(skip).Take(pageSize).ToListAsync();

        ViewBag.PeutCreer = isAdmin || await EstScoutChef();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(demandes.Select(ToDto).ToList());
    }

    /// <summary>
    /// Vérifie si l'utilisateur courant est un scout avec une fonction d'encadrement (chef)
    /// </summary>
    private async Task<bool> EstScoutChef()
    {
        if (!User.IsInRole("Scout")) return false;
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var scout = await db.Scouts.FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        if (scout?.Fonction is null) return false;
        var fonctionUpper = scout.Fonction.ToUpperInvariant();
        return fonctionUpper.Contains("CHEF") || fonctionUpper.Contains("COMMISSAIRE")
            || fonctionUpper.Contains("ACD") || fonctionUpper.Contains("ACG")
            || fonctionUpper.Contains("RESPONSABLE");
    }

    public async Task<IActionResult> Create()
    {
        // Seuls les admins/gestionnaires et les scouts chefs peuvent créer
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        if (!isAdmin && !await EstScoutChef())
            return Forbid();

        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DemandeAutorisationCreateDto dto)
    {
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        if (!isAdmin && !await EstScoutChef())
            return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).ToListAsync();
            return View(dto);
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var user = await userManager.FindByIdAsync(userId.ToString());

        var demande = new DemandeAutorisation
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre,
            Description = dto.Description,
            TypeActivite = dto.TypeActivite,
            DateActivite = DateTime.SpecifyKind(dto.DateActivite, DateTimeKind.Utc),
            DateFin = dto.DateFin.HasValue ? DateTime.SpecifyKind(dto.DateFin.Value, DateTimeKind.Utc) : null,
            Lieu = dto.Lieu,
            NombreParticipants = dto.NombreParticipants,
            Objectifs = dto.Objectifs,
            MoyensLogistiques = dto.MoyensLogistiques,
            Budget = dto.Budget,
            Observations = dto.Observations,
            GroupeId = dto.GroupeId,
            DemandeurId = userId,
            Statut = StatutDemande.Initialisee
        };

        // Générer TDR
        demande.TdrContenu = GenererTdr(demande, user);

        db.DemandesAutorisation.Add(demande);
        AjouterSuivi(demande, StatutDemande.Initialisee, StatutDemande.Initialisee, "Demande créée", user?.Prenom + " " + user?.Nom);
        await db.SaveChangesAsync();

        TempData["Success"] = "Demande d'autorisation créée. Le TDR a été généré.";
        return RedirectToAction(nameof(Details), new { id = demande.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var demande = await db.DemandesAutorisation
            .Include(d => d.Demandeur)
            .Include(d => d.Valideur)
            .Include(d => d.Groupe)
            .Include(d => d.Suivis)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null) return NotFound();

        // Les scouts ne voient que leurs propres demandes
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var isSupervision = User.IsInRole("Superviseur") || User.IsInRole("Consultant");
        if (!isAdmin && !isSupervision)
        {
            var userId = Guid.Parse(userManager.GetUserId(User)!);
            if (demande.DemandeurId != userId) return Forbid();
        }

        return View(ToDto(demande));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,Scout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        if (!isAdmin && !await EstScoutChef())
            return Forbid();

        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null) return NotFound();
        if (demande.Statut != StatutDemande.Initialisee) return BadRequest();

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        demande.Statut = StatutDemande.Soumise;
        AjouterSuivi(demande, ancien, StatutDemande.Soumise, "Demande soumise pour validation", user?.Prenom + " " + user?.Nom);
        await db.SaveChangesAsync();

        await hub.Clients.All.SendAsync("RecevoirNotification", $"Nouvelle demande d'autorisation : {demande.Titre}");
        TempData["Success"] = "Demande soumise pour validation.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        demande.Statut = StatutDemande.Validee;
        demande.ValideurId = user?.Id;
        demande.DateValidation = DateTime.UtcNow;
        AjouterSuivi(demande, ancien, StatutDemande.Validee, commentaire ?? "Demande validée", user?.Prenom + " " + user?.Nom);
        await db.SaveChangesAsync();

        await hub.Clients.User(demande.DemandeurId.ToString()).SendAsync("RecevoirNotification", $"Votre demande \"{demande.Titre}\" a été validée.");
        TempData["Success"] = "Demande validée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Rejeter(Guid id, string? motif)
    {
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        demande.Statut = StatutDemande.Rejetee;
        demande.MotifRejet = motif;
        demande.ValideurId = user?.Id;
        demande.DateValidation = DateTime.UtcNow;
        AjouterSuivi(demande, ancien, StatutDemande.Rejetee, motif ?? "Demande rejetée", user?.Prenom + " " + user?.Nom);
        await db.SaveChangesAsync();

        await hub.Clients.User(demande.DemandeurId.ToString()).SendAsync("RecevoirNotification", $"Votre demande \"{demande.Titre}\" a été rejetée.");
        TempData["Success"] = "Demande rejetée.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Reviser(Guid id, string? commentaire)
    {
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        demande.Statut = StatutDemande.EnRevision;
        AjouterSuivi(demande, ancien, StatutDemande.EnRevision, commentaire ?? "Demande renvoyée pour révision", user?.Prenom + " " + user?.Nom);
        await db.SaveChangesAsync();

        await hub.Clients.User(demande.DemandeurId.ToString()).SendAsync("RecevoirNotification", $"Votre demande \"{demande.Titre}\" nécessite des modifications.");
        TempData["Success"] = "Demande renvoyée pour révision.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static string GenererTdr(DemandeAutorisation d, ApplicationUser? user)
    {
        return $"""
        TERMES DE RÉFÉRENCE (TDR)
        ========================
        Titre : {d.Titre}
        Type d'activité : {d.TypeActivite}
        Demandeur : {user?.Prenom} {user?.Nom}
        Date : {d.DateActivite:dd/MM/yyyy}{(d.DateFin.HasValue ? $" au {d.DateFin:dd/MM/yyyy}" : "")}
        Lieu : {d.Lieu ?? "À préciser"}
        Nombre de participants : {d.NombreParticipants}

        1. CONTEXTE ET JUSTIFICATION
        {d.Description ?? "À compléter"}

        2. OBJECTIFS
        {d.Objectifs ?? "À compléter"}

        3. MOYENS LOGISTIQUES
        {d.MoyensLogistiques ?? "À compléter"}

        4. BUDGET PRÉVISIONNEL
        {d.Budget ?? "À compléter"}

        5. OBSERVATIONS
        {d.Observations ?? "Aucune"}
        """;
    }

    private void AjouterSuivi(DemandeAutorisation demande, StatutDemande ancien, StatutDemande nouveau, string? commentaire, string? auteur)
    {
        db.SuivisDemande.Add(new SuiviDemande
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            AncienStatut = ancien,
            NouveauStatut = nouveau,
            Commentaire = commentaire,
            Auteur = auteur
        });
    }

    private static DemandeAutorisationDto ToDto(DemandeAutorisation d) => new()
    {
        Id = d.Id,
        Titre = d.Titre,
        Description = d.Description,
        TypeActivite = d.TypeActivite,
        DateActivite = d.DateActivite,
        DateFin = d.DateFin,
        Lieu = d.Lieu,
        NombreParticipants = d.NombreParticipants,
        Objectifs = d.Objectifs,
        MoyensLogistiques = d.MoyensLogistiques,
        Budget = d.Budget,
        Observations = d.Observations,
        TdrContenu = d.TdrContenu,
        Statut = d.Statut,
        MotifRejet = d.MotifRejet,
        DateCreation = d.DateCreation,
        DateValidation = d.DateValidation,
        NomDemandeur = d.Demandeur != null ? $"{d.Demandeur.Prenom} {d.Demandeur.Nom}" : null,
        NomValideur = d.Valideur != null ? $"{d.Valideur.Prenom} {d.Valideur.Nom}" : null,
        NomGroupe = d.Groupe?.Nom,
        GroupeId = d.GroupeId,
        Suivis = d.Suivis.OrderBy(s => s.Date).Select(s => new SuiviDemandeDto
        {
            AncienStatut = s.AncienStatut,
            NouveauStatut = s.NouveauStatut,
            Commentaire = s.Commentaire,
            Auteur = s.Auteur,
            Date = s.Date
        }).ToList()
    };
}
