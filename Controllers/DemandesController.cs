using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        var isDistrictReviewer = await EstValidateurDistrictAsync();

        var query = db.DemandesAutorisation
            .Include(d => d.Demandeur)
            .Include(d => d.Groupe)
            .Include(d => d.Branche)
            .AsQueryable();

        if (!isAdmin && !isSupervision && !isDistrictReviewer)
        {
            query = query.Where(d => d.DemandeurId == userId);
        }

        var (page, ps) = ListPagination.Read(Request);
        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var demandes = await query
            .OrderByDescending(d => d.DateCreation)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.PeutCreer = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || await EstScoutChefAsync();
        ViewBag.PeutValiderDistrict = isDistrictReviewer;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(demandes.Select(ToDto).ToList());
    }

    public async Task<IActionResult> Create()
    {
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var currentScout = await GetCurrentScoutAsync();
        if (!isAdmin && !IsLeadershipScout(currentScout))
        {
            return Forbid();
        }

        await LoadFormDataAsync(currentScout, currentScout?.GroupeId);
        return View(new DemandeAutorisationCreateDto
        {
            DateActivite = DateTime.Today,
            GroupeId = !isAdmin ? currentScout?.GroupeId : null,
            BrancheId = null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DemandeAutorisationCreateDto dto)
    {
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var currentScout = await GetCurrentScoutAsync();
        if (!isAdmin && !IsLeadershipScout(currentScout))
        {
            return Forbid();
        }

        await ValiderCreationAsync(dto, isAdmin, currentScout);
        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(currentScout, dto.GroupeId);
            return View(dto);
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var user = await userManager.FindByIdAsync(userId.ToString());
        var groupe = dto.GroupeId.HasValue
            ? await db.Groupes.FirstOrDefaultAsync(g => g.Id == dto.GroupeId.Value)
            : null;
        var branche = dto.BrancheId.HasValue
            ? await db.Branches.FirstOrDefaultAsync(b => b.Id == dto.BrancheId.Value)
            : null;

        var demande = new DemandeAutorisation
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre.Trim(),
            Description = NormalizeMultiline(dto.Description),
            TypeActivite = dto.TypeActivite,
            DateActivite = DateTime.SpecifyKind(dto.DateActivite.Date, DateTimeKind.Utc),
            DateFin = dto.DateFin.HasValue ? DateTime.SpecifyKind(dto.DateFin.Value.Date, DateTimeKind.Utc) : null,
            Lieu = NormalizeValue(dto.Lieu),
            NombreParticipants = dto.NombreParticipants,
            Objectifs = NormalizeMultiline(dto.Objectifs),
            Responsables = NormalizeMultiline(dto.Responsables),
            MoyensLogistiques = NormalizeMultiline(dto.MoyensLogistiques),
            Budget = NormalizeValue(dto.Budget),
            Observations = NormalizeMultiline(dto.Observations),
            GroupeId = dto.GroupeId,
            BrancheId = dto.BrancheId,
            DemandeurId = userId,
            Statut = StatutDemande.Initialisee,
            Groupe = groupe,
            Branche = branche
        };

        demande.TdrContenu = GenererTdr(demande, user);

        db.DemandesAutorisation.Add(demande);
        AjouterSuivi(demande, StatutDemande.Initialisee, StatutDemande.Initialisee, "Demande creee", BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        TempData["Success"] = "Demande d'autorisation creee. Elle peut maintenant etre soumise au commissaire de district.";
        return RedirectToAction(nameof(Details), new { id = demande.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var demande = await db.DemandesAutorisation
            .Include(d => d.Demandeur)
            .Include(d => d.Valideur)
            .Include(d => d.Groupe)
            .Include(d => d.Branche)
            .Include(d => d.Suivis)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var isSupervision = User.IsInRole("Superviseur") || User.IsInRole("Consultant");
        var isDistrictReviewer = await EstValidateurDistrictAsync();
        if (!isAdmin && !isSupervision && !isDistrictReviewer)
        {
            var userId = Guid.Parse(userManager.GetUserId(User)!);
            if (demande.DemandeurId != userId)
            {
                return Forbid();
            }
        }

        ViewBag.CanValidateDistrict = isDistrictReviewer;
        ViewBag.CanSubmit = isAdmin || demande.DemandeurId == Guid.Parse(userManager.GetUserId(User)!);
        return View(ToDto(demande));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,Scout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(Guid id)
    {
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var currentScout = await GetCurrentScoutAsync();
        if (!isAdmin && !IsLeadershipScout(currentScout))
        {
            return Forbid();
        }

        var currentUserId = Guid.Parse(userManager.GetUserId(User)!);
        if (!isAdmin && demande.DemandeurId != currentUserId)
        {
            return Forbid();
        }

        if (demande.Statut != StatutDemande.Initialisee && demande.Statut != StatutDemande.EnRevision)
        {
            return BadRequest();
        }

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        demande.Statut = StatutDemande.Soumise;
        demande.MotifRejet = null;
        AjouterSuivi(demande, ancien, StatutDemande.Soumise, "Demande soumise pour validation district", BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        await hub.Clients.All.SendAsync("RecevoirNotification", $"Nouvelle demande d'autorisation d'activite : {demande.Titre}");
        TempData["Success"] = "Demande soumise au commissaire de district.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        if (!await EstValidateurDistrictAsync())
        {
            return Forbid();
        }

        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        if (demande.Statut != StatutDemande.Soumise)
        {
            return BadRequest();
        }

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        demande.Statut = StatutDemande.Validee;
        demande.ValideurId = user?.Id;
        demande.DateValidation = DateTime.UtcNow;
        demande.MotifRejet = null;
        AjouterSuivi(demande, ancien, StatutDemande.Validee, NormalizeValue(commentaire) ?? "Demande validee par le commissaire de district", BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        await hub.Clients.User(demande.DemandeurId.ToString()).SendAsync("RecevoirNotification", $"Votre demande \"{demande.Titre}\" a ete validee.");
        TempData["Success"] = "Demande validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(Guid id, string? motif)
    {
        if (!await EstValidateurDistrictAsync())
        {
            return Forbid();
        }

        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        if (demande.Statut != StatutDemande.Soumise)
        {
            return BadRequest();
        }

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        var motifNormalise = NormalizeValue(motif) ?? "Demande rejetee";
        demande.Statut = StatutDemande.Rejetee;
        demande.MotifRejet = motifNormalise;
        demande.ValideurId = user?.Id;
        demande.DateValidation = DateTime.UtcNow;
        AjouterSuivi(demande, ancien, StatutDemande.Rejetee, motifNormalise, BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        await hub.Clients.User(demande.DemandeurId.ToString()).SendAsync("RecevoirNotification", $"Votre demande \"{demande.Titre}\" a ete rejetee.");
        TempData["Success"] = "Demande rejetee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reviser(Guid id, string? commentaire)
    {
        if (!await EstValidateurDistrictAsync())
        {
            return Forbid();
        }

        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        if (demande.Statut != StatutDemande.Soumise)
        {
            return BadRequest();
        }

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        var commentaireNormalise = NormalizeValue(commentaire) ?? "Modification demandee par le commissaire de district";
        demande.Statut = StatutDemande.EnRevision;
        demande.MotifRejet = null;
        demande.ValideurId = user?.Id;
        AjouterSuivi(demande, ancien, StatutDemande.EnRevision, commentaireNormalise, BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        await hub.Clients.User(demande.DemandeurId.ToString()).SendAsync("RecevoirNotification", $"Votre demande \"{demande.Titre}\" doit etre modifiee avant une nouvelle validation.");
        TempData["Success"] = "Demande renvoyee pour modification.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadFormDataAsync(Scout? currentScout, Guid? selectedGroupeId)
    {
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");

        var groupesQuery = db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).AsQueryable();
        if (!isAdmin && currentScout?.GroupeId is Guid scoutGroupeId)
        {
            groupesQuery = groupesQuery.Where(g => g.Id == scoutGroupeId);
            selectedGroupeId ??= scoutGroupeId;
        }

        var branchesQuery = db.Branches.Where(b => b.IsActive).OrderBy(b => b.Nom).AsQueryable();
        if (!isAdmin && currentScout?.GroupeId is Guid lockedGroupeId)
        {
            branchesQuery = branchesQuery.Where(b => b.GroupeId == lockedGroupeId);
        }

        ViewBag.Groupes = await groupesQuery.ToListAsync();
        ViewBag.Branches = await branchesQuery.ToListAsync();
        ViewBag.GroupeVerrouille = !isAdmin && currentScout?.GroupeId is not null;
        ViewBag.GroupeSelectionne = selectedGroupeId;
    }

    private async Task ValiderCreationAsync(DemandeAutorisationCreateDto dto, bool isAdmin, Scout? currentScout)
    {
        if (!dto.GroupeId.HasValue)
        {
            ModelState.AddModelError(nameof(dto.GroupeId), "Le groupe concerne est obligatoire.");
        }

        if (!dto.BrancheId.HasValue)
        {
            ModelState.AddModelError(nameof(dto.BrancheId), "La branche concernee est obligatoire.");
        }

        Groupe? groupe = null;
        if (dto.GroupeId.HasValue)
        {
            groupe = await db.Groupes.FirstOrDefaultAsync(g => g.Id == dto.GroupeId.Value && g.IsActive);
            if (groupe is null)
            {
                ModelState.AddModelError(nameof(dto.GroupeId), "Le groupe selectionne est introuvable ou inactif.");
            }
        }

        Branche? branche = null;
        if (dto.BrancheId.HasValue)
        {
            branche = await db.Branches.FirstOrDefaultAsync(b => b.Id == dto.BrancheId.Value && b.IsActive);
            if (branche is null)
            {
                ModelState.AddModelError(nameof(dto.BrancheId), "La branche selectionnee est introuvable ou inactive.");
            }
        }

        if (groupe is not null && branche is not null && branche.GroupeId != groupe.Id)
        {
            ModelState.AddModelError(nameof(dto.BrancheId), "La branche concernee doit appartenir au groupe selectionne.");
        }

        if (!isAdmin)
        {
            if (currentScout?.GroupeId is null)
            {
                ModelState.AddModelError(nameof(dto.GroupeId), "Votre profil scout n'est rattache a aucun groupe.");
            }
            else if (dto.GroupeId != currentScout.GroupeId)
            {
                ModelState.AddModelError(nameof(dto.GroupeId), "Un chef de groupe ne peut soumettre que des demandes pour son propre groupe.");
            }
        }
    }

    private async Task<Scout?> GetCurrentScoutAsync()
    {
        var userId = userManager.GetUserId(User);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        return await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.UserId == parsedUserId && s.IsActive);
    }

    private async Task<bool> EstScoutChefAsync()
    {
        var scout = await GetCurrentScoutAsync();
        return IsLeadershipScout(scout);
    }

    private async Task<bool> EstValidateurDistrictAsync()
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null || scout.Groupe is null)
        {
            return false;
        }

        if (!IsDistrictEquipe(scout.Groupe.Nom))
        {
            return false;
        }

        return IsDistrictValidationFunction(scout.Fonction);
    }

    private static bool IsLeadershipScout(Scout? scout)
    {
        if (scout is null)
        {
            return false;
        }

        var normalizedFunction = DatabaseText.NormalizeSearchKey(scout.Fonction);
        return normalizedFunction.Contains("CHEF", StringComparison.Ordinal)
            || normalizedFunction.Contains("COMMISSAIRE", StringComparison.Ordinal)
            || normalizedFunction.Contains("RESPONSABLE", StringComparison.Ordinal);
    }

    private static bool IsDistrictEquipe(string? groupeNom)
    {
        return DatabaseText.NormalizeSearchKey(groupeNom)
            == DatabaseText.NormalizeSearchKey("Equipe de District Mango Taika");
    }

    private static bool IsDistrictValidationFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction);
        return normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT (CD)")
            || normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT ADJOINT (CDA)")
            || normalizedFunction == DatabaseText.NormalizeSearchKey("ASSISTANT COMMISSAIRE DE DISTRICT (ACD)");
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeMultiline(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildAuteurLabel(ApplicationUser? user)
    {
        return user is null ? "Systeme" : $"{user.Prenom} {user.Nom}".Trim();
    }

    private static string GenererTdr(DemandeAutorisation d, ApplicationUser? user)
    {
        return $"""
        TERMES DE REFERENCE (TDR)
        ========================
        Nom de l'activite : {d.Titre}
        Type d'activite : {d.TypeActivite}
        Demandeur : {user?.Prenom} {user?.Nom}
        Groupe concerne : {d.Groupe?.Nom ?? "A preciser"}
        Branche concernee : {d.Branche?.Nom ?? "A preciser"}
        Date : {d.DateActivite:dd/MM/yyyy}{(d.DateFin.HasValue ? $" au {d.DateFin:dd/MM/yyyy}" : string.Empty)}
        Lieu : {d.Lieu ?? "A preciser"}
        Nombre de participants : {d.NombreParticipants}
        Responsables : {d.Responsables ?? "A preciser"}

        1. CONTEXTE ET JUSTIFICATION
        {d.Description ?? "A completer"}

        2. OBJECTIF
        {d.Objectifs ?? "A completer"}

        3. MOYENS LOGISTIQUES
        {d.MoyensLogistiques ?? "A completer"}

        4. BUDGET PREVISIONNEL
        {d.Budget ?? "A completer"}

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
        Responsables = d.Responsables,
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
        NomBranche = d.Branche?.Nom,
        BrancheId = d.BrancheId,
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
