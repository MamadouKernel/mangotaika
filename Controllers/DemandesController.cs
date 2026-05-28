using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MangoTaika.Controllers;

[Authorize]
public class DemandesController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationDispatchService notificationDispatchService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var isAdmin = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        var isSupervision = User.IsInRole("Superviseur") || User.IsInRole("Consultant");
        var isDistrictReviewer = await EstValidateurDistrictAsync();
        var chefGroupeScope = await GetChefGroupeValidationScopeAsync();
        var canValidateRequests = User.IsInRole("Administrateur") || isDistrictReviewer || chefGroupeScope.HasValue;

        var query = db.DemandesAutorisation
            .Include(d => d.Demandeur)
            .Include(d => d.Groupe)
            .Include(d => d.Branche)
            .AsQueryable();

        if (!isAdmin && !isSupervision && !canValidateRequests)
        {
            query = query.Where(d => d.DemandeurId == userId);
        }
        else if (!isAdmin && !isSupervision && !isDistrictReviewer && chefGroupeScope.HasValue)
        {
            query = query.Where(d => d.GroupeId == chefGroupeScope.Value);
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
        ViewBag.PeutValiderDistrict = canValidateRequests;
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
        var canValidateRequests = User.IsInRole("Administrateur") || isDistrictReviewer || await CanChefGroupeValidateAsync(demande);
        if (!isAdmin && !isSupervision && !canValidateRequests)
        {
            var userId = Guid.Parse(userManager.GetUserId(User)!);
            if (demande.DemandeurId != userId)
            {
                return Forbid();
            }
        }

        ViewBag.CanValidateDistrict = canValidateRequests;
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
        var chefUniteWorkflow = await IsChefUniteRequestAsync(demande);
        AjouterSuivi(demande, ancien, StatutDemande.Soumise, chefUniteWorkflow ? "Demande soumise pour validation du chef de groupe" : "Demande soumise pour validation district", BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        var validationRecipients = await GetValidationRecipientIdsAsync(demande, chefUniteWorkflow);
        await notificationDispatchService.SendAsync(
            validationRecipients,
            "Nouvelle demande d'autorisation",
            $"Nouvelle demande d'autorisation d'activite : {demande.Titre}",
            "Demandes",
            Url.Action(nameof(Details), "Demandes", new { id }, Request.Scheme));

        TempData["Success"] = chefUniteWorkflow ? "Demande soumise au chef de groupe." : "Demande soumise au commissaire de district.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        var isPlatformAdmin = User.IsInRole("Administrateur");
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        if (!isPlatformAdmin && !await EstValidateurDistrictAsync() && !await CanChefGroupeValidateAsync(demande))
        {
            return Forbid();
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
        var commentaireValidation = NormalizeValue(commentaire)
            ?? (await IsChefUniteRequestAsync(demande) && await CanChefGroupeValidateAsync(demande)
                ? "Demande validee par le chef de groupe"
                : isPlatformAdmin ? "Validation exceptionnelle par administrateur" : "Demande validee par le commissaire de district");
        AjouterSuivi(demande, ancien, StatutDemande.Validee, commentaireValidation, BuildAuteurLabel(user));
        var activite = await CreerActiviteDepuisDemandeAsync(demande, user?.Id ?? demande.DemandeurId);
        await db.SaveChangesAsync();

        await notificationDispatchService.SendAsync(
            [demande.DemandeurId],
            "Demande validee",
            $"Votre demande \"{demande.Titre}\" a ete validee.",
            "Demandes",
            activite is not null
                ? Url.Action("Details", "Activites", new { id = activite.Id }, Request.Scheme)
                : Url.Action(nameof(Details), "Demandes", new { id }, Request.Scheme));

        TempData["Success"] = "Demande validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(Guid id, string? motif)
    {
        var isPlatformAdmin = User.IsInRole("Administrateur");
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        if (!isPlatformAdmin && !await EstValidateurDistrictAsync() && !await CanChefGroupeValidateAsync(demande))
        {
            return Forbid();
        }

        if (demande.Statut != StatutDemande.Soumise)
        {
            return BadRequest();
        }

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        var motifNormalise = NormalizeValue(motif)
            ?? (await IsChefUniteRequestAsync(demande) && await CanChefGroupeValidateAsync(demande)
                ? "Demande rejetee par le chef de groupe"
                : isPlatformAdmin ? "Rejet exceptionnel par administrateur" : "Demande rejetee");
        demande.Statut = StatutDemande.Rejetee;
        demande.MotifRejet = motifNormalise;
        demande.ValideurId = user?.Id;
        demande.DateValidation = DateTime.UtcNow;
        AjouterSuivi(demande, ancien, StatutDemande.Rejetee, motifNormalise, BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        await notificationDispatchService.SendAsync(
            [demande.DemandeurId],
            "Demande rejetee",
            $"Votre demande \"{demande.Titre}\" a ete rejetee. Motif : {motifNormalise}",
            "Demandes",
            Url.Action(nameof(Details), "Demandes", new { id }, Request.Scheme));

        TempData["Success"] = "Demande rejetee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reviser(Guid id, string? commentaire)
    {
        var isPlatformAdmin = User.IsInRole("Administrateur");
        var demande = await db.DemandesAutorisation.Include(d => d.Suivis).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
        {
            return NotFound();
        }

        if (!isPlatformAdmin && !await EstValidateurDistrictAsync() && !await CanChefGroupeValidateAsync(demande))
        {
            return Forbid();
        }

        if (demande.Statut != StatutDemande.Soumise)
        {
            return BadRequest();
        }

        var user = await userManager.GetUserAsync(User);
        var ancien = demande.Statut;
        var commentaireNormalise = NormalizeValue(commentaire)
            ?? (await IsChefUniteRequestAsync(demande) && await CanChefGroupeValidateAsync(demande)
                ? "Modification demandee par le chef de groupe"
                : isPlatformAdmin ? "Modification exceptionnelle demandee par administrateur" : "Modification demandee par le commissaire de district");
        demande.Statut = StatutDemande.EnRevision;
        demande.MotifRejet = null;
        demande.ValideurId = user?.Id;
        AjouterSuivi(demande, ancien, StatutDemande.EnRevision, commentaireNormalise, BuildAuteurLabel(user));
        await db.SaveChangesAsync();

        await notificationDispatchService.SendAsync(
            [demande.DemandeurId],
            "Demande a reviser",
            $"Votre demande \"{demande.Titre}\" doit etre modifiee avant une nouvelle validation. Commentaire : {commentaireNormalise}",
            "Demandes",
            Url.Action(nameof(Details), "Demandes", new { id }, Request.Scheme));

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
        if (!isAdmin && IsChefUniteFunction(currentScout?.Fonction) && currentScout?.BrancheId is Guid lockedBrancheId)
        {
            branchesQuery = branchesQuery.Where(b => b.Id == lockedBrancheId);
        }
        else if (!isAdmin && currentScout?.GroupeId is Guid lockedGroupeId)
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
            ModelState.AddModelError(nameof(dto.GroupeId), "Selectionnez le groupe concerne par la demande. Cette information est necessaire pour le suivi et la validation.");
        }

        Groupe? groupe = null;
        if (dto.GroupeId.HasValue)
        {
            groupe = await db.Groupes.FirstOrDefaultAsync(g => g.Id == dto.GroupeId.Value && g.IsActive);
            if (groupe is null)
            {
                ModelState.AddModelError(nameof(dto.GroupeId), "Le groupe selectionne est introuvable ou inactif. Choisissez un groupe actif ou contactez l'administration.");
            }
        }

        Branche? branche = null;
        if (dto.BrancheId.HasValue)
        {
            branche = await db.Branches.FirstOrDefaultAsync(b => b.Id == dto.BrancheId.Value && b.IsActive);
            if (branche is null)
            {
                ModelState.AddModelError(nameof(dto.BrancheId), "La branche selectionnee est introuvable ou inactive. Choisissez une branche active du groupe.");
            }
        }

        if (groupe is not null && branche is not null && branche.GroupeId != groupe.Id)
        {
            ModelState.AddModelError(nameof(dto.BrancheId), "La branche choisie n'appartient pas au groupe selectionne. Selectionnez une branche du meme groupe.");
        }

        if (!isAdmin)
        {
            if (currentScout?.GroupeId is null)
            {
                ModelState.AddModelError(nameof(dto.GroupeId), "Votre profil scout n'est rattache a aucun groupe.");
            }
            else if (dto.GroupeId != currentScout.GroupeId)
            {
                ModelState.AddModelError(nameof(dto.GroupeId), "Vous ne pouvez soumettre que des demandes pour votre propre groupe.");
            }

            if (IsChefUniteFunction(currentScout?.Fonction))
            {
                if (!dto.BrancheId.HasValue)
                {
                    ModelState.AddModelError(nameof(dto.BrancheId), "La branche est obligatoire pour une demande creee par un chef d'unite.");
                }
                else if (dto.BrancheId != currentScout?.BrancheId)
                {
                    ModelState.AddModelError(nameof(dto.BrancheId), "Un chef d'unite ne peut soumettre que des demandes pour sa branche.");
                }
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

    private async Task<List<Guid>> GetValidationRecipientIdsAsync(DemandeAutorisation demande, bool chefUniteWorkflow)
    {
        if (chefUniteWorkflow && demande.GroupeId.HasValue)
        {
            var chefGroupeUsers = await db.Scouts
                .Where(s => s.IsActive && s.UserId.HasValue && s.GroupeId == demande.GroupeId.Value)
                .Where(s => s.Fonction != null)
                .Select(s => new { s.UserId, s.Fonction })
                .ToListAsync();

            return chefGroupeUsers
                .Where(s => IsChefGroupeFunction(s.Fonction))
                .Select(s => s.UserId!.Value)
                .Distinct()
                .ToList();
        }

        var roleRecipients = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == RoleNames.Administrateur
                || x.Name == RoleNames.Gestionnaire
                || x.Name == RoleNames.CommissaireDistrict)
            .Select(x => x.UserId)
            .ToListAsync();

        var districtReviewers = await db.Scouts
            .Include(s => s.Groupe)
            .Where(s => s.IsActive && s.UserId.HasValue)
            .Where(s => s.Groupe != null && s.Groupe.Nom == "Equipe de District Mango Taika")
            .Select(s => new { s.UserId, s.Fonction })
            .ToListAsync();

        return roleRecipients
            .Concat(districtReviewers
                .Where(s => IsDistrictValidationFunction(s.Fonction))
                .Select(s => s.UserId!.Value))
            .Distinct()
            .ToList();
    }

    private async Task<Guid?> GetChefGroupeValidationScopeAsync()
    {
        if (!User.IsInRole(RoleNames.ChefGroupe))
        {
            return null;
        }

        var scout = await GetCurrentScoutAsync();
        return scout is not null && IsChefGroupeFunction(scout.Fonction) ? scout.GroupeId : null;
    }

    private async Task<bool> CanChefGroupeValidateAsync(DemandeAutorisation demande)
    {
        var scopeGroupeId = await GetChefGroupeValidationScopeAsync();
        return scopeGroupeId.HasValue
            && demande.GroupeId == scopeGroupeId.Value
            && await IsChefUniteRequestAsync(demande);
    }

    private async Task<bool> IsChefUniteRequestAsync(DemandeAutorisation demande)
    {
        var demandeurScout = await db.Scouts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == demande.DemandeurId && s.IsActive);
        return IsChefUniteFunction(demandeurScout?.Fonction);
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

    private static bool IsChefGroupeFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction);
        return normalizedFunction.Contains("CHEF DE GROUPE", StringComparison.Ordinal)
            || normalizedFunction.Contains("CHEF GROUPE", StringComparison.Ordinal);
    }

    private static bool IsChefUniteFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction);
        return normalizedFunction.Contains("CHEF D UNITE", StringComparison.Ordinal)
            || normalizedFunction.Contains("CHEF UNITE", StringComparison.Ordinal);
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
        Branche concernee : {d.Branche?.Nom ?? "Tout le groupe"}
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

    private async Task<Activite?> CreerActiviteDepuisDemandeAsync(DemandeAutorisation demande, Guid createurId)
    {
        var existeDeja = await db.Activites.AnyAsync(a =>
            !a.EstSupprime
            && a.GroupeId == demande.GroupeId
            && a.Titre == demande.Titre
            && a.DateDebut == demande.DateActivite);

        if (existeDeja)
        {
            return null;
        }

        var activite = new Activite
        {
            Id = Guid.NewGuid(),
            Titre = demande.Titre,
            Description = demande.Description,
            Type = MapTypeActivite(demande.TypeActivite),
            DateDebut = demande.DateActivite,
            DateFin = demande.DateFin,
            Lieu = demande.Lieu,
            BudgetPrevisionnel = TryParseBudget(demande.Budget),
            NomResponsable = demande.Responsables,
            Statut = StatutActivite.Validee,
            CreateurId = demande.DemandeurId,
            GroupeId = demande.GroupeId
        };

        db.Activites.Add(activite);
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = activite.Id,
            AuteurId = createurId,
            Contenu = $"Activite generee automatiquement apres validation de la demande {demande.Titre}.",
            TypeAction = "Validation"
        });

        AjouterSuivi(demande, StatutDemande.Validee, StatutDemande.Validee, $"Activite creee automatiquement : {activite.Titre}", "Systeme");
        return activite;
    }

    private static TypeActivite MapTypeActivite(TypeActiviteDemande type)
        => type switch
        {
            TypeActiviteDemande.Sortie => TypeActivite.Sortie,
            TypeActiviteDemande.Camp => TypeActivite.Camp,
            TypeActiviteDemande.Formation => TypeActivite.Formation,
            TypeActiviteDemande.Ceremonie => TypeActivite.Ceremonie,
            _ => TypeActivite.Autre
        };

    private static decimal? TryParseBudget(string? budget)
    {
        if (string.IsNullOrWhiteSpace(budget)) return null;
        var digits = new string(budget.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray()).Replace(',', '.');
        return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
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


