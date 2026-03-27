using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MangoTaika.Services;

public class DashboardService(AppDbContext db, IFormationService formationService) : IDashboardService
{
    public async Task<DashboardDto> GetDashboardAsync(ClaimsPrincipal user)
    {
        var dto = new DashboardDto();
        var userId = TryGetUserId(user);

        if (user.IsInRole("Administrateur") || user.IsInRole("Gestionnaire"))
        {
            dto.RoleActif = user.IsInRole("Administrateur") ? "Administrateur" : "Gestionnaire";
            dto.TitreBienvenue = "Pilotage global";
            dto.SousTitreBienvenue = "Vue complete des operations du district et des demandes en attente.";
            await FillEncadrementDashboardAsync(dto, includeMessages: true, includePartenaires: true);
            return dto;
        }

        if (user.IsInRole("AgentSupport"))
        {
            var now = DateTime.UtcNow;
            dto.RoleActif = "Agent support";
            dto.TitreBienvenue = "File de support";
            dto.SousTitreBienvenue = "Vue operationnelle des tickets assignes, du SLA et de la base de resolution.";
            dto.TicketsOuverts = await db.Tickets.CountAsync(t => !t.EstSupprime && (t.Statut == StatutTicket.Ouvert || t.Statut == StatutTicket.Nouveau));
            dto.ActivitesEnCours = await db.Tickets.CountAsync(t => !t.EstSupprime && (t.Statut == StatutTicket.Affecte || t.Statut == StatutTicket.EnCours));
            dto.DemandesAutorisationEnAttente = await db.Tickets.CountAsync(t => !t.EstSupprime && (t.Statut == StatutTicket.EnAttenteDemandeur || t.Statut == StatutTicket.EnAttenteTiers || t.Statut == StatutTicket.EnAttente));
            dto.MesTicketsAssignes = userId.HasValue
                ? await db.Tickets.CountAsync(t => !t.EstSupprime && t.AssigneAId == userId.Value && t.Statut != StatutTicket.Resolu && t.Statut != StatutTicket.Ferme && t.Statut != StatutTicket.Annule)
                : 0;
            dto.TicketsEscalades = await db.Tickets.CountAsync(t => !t.EstSupprime && t.EstEscalade && t.Statut != StatutTicket.Resolu && t.Statut != StatutTicket.Ferme && t.Statut != StatutTicket.Annule);
            dto.TicketsEnRetardSla = await db.Tickets.CountAsync(t => !t.EstSupprime && t.Statut != StatutTicket.Resolu && t.Statut != StatutTicket.Ferme && t.Statut != StatutTicket.Annule && t.DateLimiteSla < now);
            dto.NotificationsNonLues = userId.HasValue
                ? await db.NotificationsUtilisateur.CountAsync(n => n.UserId == userId.Value && !n.EstLue)
                : 0;
            dto.TotalCompetences = await db.SupportCatalogueServices.CountAsync(s => s.EstActif);
            dto.TotalProjetsAGR = await db.SupportKnowledgeArticles.CountAsync(a => a.EstPublie);
            var resolvedTickets = db.Tickets.Where(t => !t.EstSupprime && (t.Statut == StatutTicket.Resolu || t.Statut == StatutTicket.Ferme) && t.DateResolution.HasValue);
            var totalResolved = await resolvedTickets.CountAsync();
            if (totalResolved > 0)
            {
                var resolvedWithinSla = await resolvedTickets.CountAsync(t => t.DateResolution <= t.DateLimiteSla);
                dto.TauxRespectSla = Math.Round((double)resolvedWithinSla * 100d / totalResolved, 1);
            }
            return dto;
        }

        if (user.IsInRole("Superviseur") || user.IsInRole("Consultant"))
        {
            dto.RoleActif = user.IsInRole("Superviseur") ? "Superviseur" : "Consultant";
            dto.TitreBienvenue = "Vue de supervision";
            dto.SousTitreBienvenue = "Indicateurs de suivi sur les modules consultables depuis votre role.";
            await FillEncadrementDashboardAsync(dto, includeMessages: false, includePartenaires: false);
            return dto;
        }

        if (user.IsInRole("Scout"))
        {
            dto.RoleActif = "Scout";
            dto.TitreBienvenue = "Mon espace scout";
            dto.SousTitreBienvenue = "Vue personnelle de vos activites, demandes, formations et tickets.";
            await FillScoutDashboardAsync(dto, userId);
            return dto;
        }

        if (user.IsInRole("Parent"))
        {
            dto.RoleActif = "Parent / Tuteur";
            dto.TitreBienvenue = "Suivi de mes enfants";
            dto.SousTitreBienvenue = "Vue des activites, formations et cotisations des scouts qui vous sont lies.";
            await FillParentDashboardAsync(dto, userId);
            return dto;
        }

        dto.RoleActif = "Utilisateur";
        dto.TitreBienvenue = "Tableau de bord";
        dto.SousTitreBienvenue = "Aucune configuration specifique n'a ete definie pour ce role.";
        return dto;
    }

    private async Task FillEncadrementDashboardAsync(DashboardDto dto, bool includeMessages, bool includePartenaires)
    {
        dto.TotalScouts = await db.Scouts.CountAsync(s => s.IsActive);
        dto.TotalGroupes = await db.Groupes.CountAsync(g => g.IsActive);
        dto.TotalBranches = await db.Branches.CountAsync(b => b.IsActive);
        dto.ActivitesEnCours = await db.Activites.CountAsync(a => !a.EstSupprime && (a.Statut == StatutActivite.Validee || a.Statut == StatutActivite.EnCours));
        dto.TicketsOuverts = await db.Tickets.CountAsync(t => !t.EstSupprime && (t.Statut == StatutTicket.Ouvert || t.Statut == StatutTicket.EnCours));
        dto.DemandesAutorisationEnAttente = await db.DemandesAutorisation.CountAsync(d => d.Statut == StatutDemande.Soumise || d.Statut == StatutDemande.EnRevision);
        dto.DemandesGroupeEnAttente = await db.DemandesGroupe.CountAsync(d => d.Statut == StatutDemandeGroupe.EnAttente);
        dto.TotalCompetences = await db.Competences.CountAsync();
        dto.TotalProjetsAGR = await db.ProjetsAGR.CountAsync(p => !p.EstSupprime);
        dto.TotalPartenaires = includePartenaires
            ? await db.Partenaires.CountAsync(p => p.EstActif && !p.EstSupprime)
            : 0;
        dto.MessagesNonLus = includeMessages
            ? await db.ContactMessages.CountAsync(m => !m.EstSupprime && !m.EstLu && m.Type == "Contact")
            : 0;
        dto.AvisNonLus = includeMessages
            ? await db.ContactMessages.CountAsync(m => !m.EstSupprime && !m.EstLu && m.Type == "Avis")
            : 0;
        dto.TotalRecettes = await db.TransactionsFinancieres
            .Where(t => !t.EstSupprime && t.Type == TypeTransaction.Recette && t.DateTransaction.Year == DateTime.UtcNow.Year)
            .SumAsync(t => t.Montant);
        dto.TotalDepenses = await db.TransactionsFinancieres
            .Where(t => !t.EstSupprime && t.Type == TypeTransaction.Depense && t.DateTransaction.Year == DateTime.UtcNow.Year)
            .SumAsync(t => t.Montant);
        dto.SoldeFinancier = dto.TotalRecettes - dto.TotalDepenses;

        dto.DerniersGroupes = await db.Groupes
            .Include(g => g.Membres)
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.DateCreation)
            .Take(5)
            .Select(g => new GroupeDto
            {
                Id = g.Id,
                Nom = g.Nom,
                Description = g.Description,
                LogoUrl = g.LogoUrl,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                Adresse = g.Adresse,
                NombreMembres = g.Membres.Count
            })
            .ToListAsync();

        dto.DernieresActivites = await db.Activites
            .Include(a => a.Groupe)
            .Where(a => !a.EstSupprime)
            .OrderByDescending(a => a.DateCreation)
            .Take(5)
            .Select(a => new ActiviteDto
            {
                Id = a.Id,
                Titre = a.Titre,
                Description = a.Description,
                DateDebut = a.DateDebut,
                DateFin = a.DateFin,
                Lieu = a.Lieu,
                Statut = a.Statut,
                NomGroupe = a.Groupe != null ? a.Groupe.Nom : null
            })
            .ToListAsync();
    }

    private async Task FillScoutDashboardAsync(DashboardDto dto, Guid? userId)
    {
        if (userId is null)
        {
            dto.MessageInfo = "Aucun compte utilisateur exploitable n'a ete trouve.";
            return;
        }

        var scout = await db.Scouts.FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        if (scout is null)
        {
            dto.MessageInfo = "Aucune fiche scout active n'est liee a votre compte.";
            return;
        }

        dto.MesActivites = await db.ParticipantsActivite
            .Where(p => p.ScoutId == scout.Id && !p.Activite.EstSupprime)
            .CountAsync();
        dto.MesDemandes = await db.DemandesAutorisation
            .Where(d => d.DemandeurId == userId && d.Statut != StatutDemande.Rejetee && d.Statut != StatutDemande.Validee)
            .CountAsync();
        dto.TicketsOuverts = await db.Tickets
            .Where(t => !t.EstSupprime && t.CreateurId == userId && (t.Statut == StatutTicket.Ouvert || t.Statut == StatutTicket.EnCours || t.Statut == StatutTicket.EnAttente))
            .CountAsync();
        dto.MesFormations = await db.InscriptionsFormation
            .Where(i => i.ScoutId == scout.Id && i.Statut == StatutInscription.EnCours)
            .CountAsync();
        dto.MesCompetences = await db.Competences
            .Where(c => c.ScoutId == scout.Id)
            .CountAsync();
        dto.MesCertificats = await db.CertificationsFormation
            .Where(c => c.ScoutId == scout.Id)
            .CountAsync();

        var parcoursLms = await formationService.GetParcoursScoutsAsync([scout.Id]);
        dto.ParcoursLms = parcoursLms
            .Take(4)
            .ToList();
        dto.ProgressionLmsMoyenne = parcoursLms.Count > 0
            ? Math.Round(parcoursLms.Average(p => p.ProgressionPourcent), 1)
            : 0;
        dto.SessionsLmsAVenir = parcoursLms.Count(p =>
            !p.EstSessionSelfPaced &&
            p.DateOuvertureSession.HasValue &&
            p.DateOuvertureSession.Value > DateTime.UtcNow);
        dto.ParcoursCertifiants = parcoursLms.Count(p => p.DelivreBadge || p.DelivreAttestation || p.DelivreCertificat);
        dto.DiscussionsLmsActives = parcoursLms.Sum(p => p.NombreDiscussions);
        dto.AnnoncesLmsActives = parcoursLms.Sum(p => p.NombreAnnonces);
        dto.DernieresFormations = dto.ParcoursLms
            .Select(p => new DashboardFormationItemDto
            {
                FormationId = p.FormationId,
                Titre = p.Titre,
                SessionTitre = p.SessionTitre,
                SessionStatut = p.SessionStatut,
                EstSessionSelfPaced = p.EstSessionSelfPaced,
                ProgressionPourcent = p.ProgressionPourcent,
                Statut = p.Statut
            })
            .ToList();

        var formationIds = dto.ParcoursLms.Select(f => f.FormationId).ToList();

        dto.DernieresAnnoncesFormation = await db.AnnoncesFormation
            .AsNoTracking()
            .Where(a => a.EstPubliee && formationIds.Contains(a.FormationId))
            .Include(a => a.Auteur)
            .OrderByDescending(a => a.DatePublication)
            .Take(4)
            .Select(a => new AnnonceFormationDto
            {
                Id = a.Id,
                Titre = a.Titre,
                Contenu = a.Contenu,
                EstPubliee = a.EstPubliee,
                DatePublication = a.DatePublication,
                NomAuteur = a.Auteur != null ? a.Auteur.Prenom + " " + a.Auteur.Nom : null
            })
            .ToListAsync();

        dto.DernieresActivites = await db.ParticipantsActivite
            .Where(p => p.ScoutId == scout.Id && !p.Activite.EstSupprime)
            .OrderByDescending(p => p.Activite.DateDebut)
            .GroupBy(p => new
            {
                p.Activite.Id,
                p.Activite.Titre,
                p.Activite.Description,
                p.Activite.DateDebut,
                p.Activite.DateFin,
                p.Activite.Lieu,
                p.Activite.Statut,
                NomGroupe = p.Activite.Groupe != null ? p.Activite.Groupe.Nom : null
            })
            .Select(g => new ActiviteDto
            {
                Id = g.Key.Id,
                Titre = g.Key.Titre,
                Description = g.Key.Description,
                DateDebut = g.Key.DateDebut,
                DateFin = g.Key.DateFin,
                Lieu = g.Key.Lieu,
                Statut = g.Key.Statut,
                NomGroupe = g.Key.NomGroupe
            })
            .OrderByDescending(a => a.DateDebut)
            .Take(5)
            .ToListAsync();
    }

    private async Task FillParentDashboardAsync(DashboardDto dto, Guid? userId)
    {
        if (userId is null)
        {
            dto.MessageInfo = "Aucun compte utilisateur exploitable n'a ete trouve.";
            return;
        }

        var userEntity = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (userEntity is null)
        {
            dto.MessageInfo = "Compte parent introuvable.";
            return;
        }

        var parent = await db.Parents
            .Include(p => p.Scouts)
            .FirstOrDefaultAsync(p => p.Telephone == userEntity.PhoneNumber);

        if (parent is null)
        {
            dto.MessageInfo = "Aucun enfant n'est lie a votre compte.";
            return;
        }

        var scoutIds = parent.Scouts
            .Where(s => s.IsActive)
            .Select(s => s.Id)
            .ToList();

        dto.MesEnfants = scoutIds.Count;
        dto.TicketsOuverts = await db.Tickets
            .Where(t => !t.EstSupprime && t.CreateurId == userId && (t.Statut == StatutTicket.Ouvert || t.Statut == StatutTicket.EnCours || t.Statut == StatutTicket.EnAttente))
            .CountAsync();

        if (scoutIds.Count == 0)
        {
            dto.MessageInfo = "Aucun enfant actif n'est actuellement rattache a votre compte.";
            return;
        }

        dto.ActivitesFamille = await db.ParticipantsActivite
            .Where(p => scoutIds.Contains(p.ScoutId) && !p.Activite.EstSupprime)
            .Select(p => p.ActiviteId)
            .Distinct()
            .CountAsync();
        dto.FormationsFamille = await db.InscriptionsFormation
            .Where(i => scoutIds.Contains(i.ScoutId) && i.Statut == StatutInscription.EnCours)
            .CountAsync();
        dto.CotisationsFamille = await db.TransactionsFinancieres
            .Where(t => !t.EstSupprime && t.ScoutId != null && scoutIds.Contains(t.ScoutId.Value) && t.Type == TypeTransaction.Recette && t.Categorie == CategorieFinance.Cotisation)
            .SumAsync(t => t.Montant);

        var parcoursLms = await formationService.GetParcoursScoutsAsync(scoutIds);
        dto.ParcoursLms = parcoursLms
            .Take(4)
            .ToList();
        dto.ProgressionLmsMoyenne = parcoursLms.Count > 0
            ? Math.Round(parcoursLms.Average(p => p.ProgressionPourcent), 1)
            : 0;
        dto.SessionsLmsAVenir = parcoursLms.Count(p =>
            !p.EstSessionSelfPaced &&
            p.DateOuvertureSession.HasValue &&
            p.DateOuvertureSession.Value > DateTime.UtcNow);
        dto.ParcoursCertifiants = parcoursLms.Count(p => p.DelivreBadge || p.DelivreAttestation || p.DelivreCertificat);
        dto.DiscussionsLmsActives = parcoursLms.Sum(p => p.NombreDiscussions);
        dto.AnnoncesLmsActives = parcoursLms.Sum(p => p.NombreAnnonces);
        dto.DernieresFormations = dto.ParcoursLms
            .Select(p => new DashboardFormationItemDto
            {
                FormationId = p.FormationId,
                Titre = p.Titre,
                SessionTitre = p.SessionTitre,
                SessionStatut = p.SessionStatut,
                EstSessionSelfPaced = p.EstSessionSelfPaced,
                ProgressionPourcent = p.ProgressionPourcent,
                Statut = p.Statut,
                NomScout = p.NomScout
            })
            .ToList();

        var formationIds = dto.ParcoursLms.Select(f => f.FormationId).Distinct().ToList();

        dto.DernieresAnnoncesFormation = await db.AnnoncesFormation
            .AsNoTracking()
            .Where(a => a.EstPubliee && formationIds.Contains(a.FormationId))
            .Include(a => a.Auteur)
            .OrderByDescending(a => a.DatePublication)
            .Take(4)
            .Select(a => new AnnonceFormationDto
            {
                Id = a.Id,
                Titre = a.Titre,
                Contenu = a.Contenu,
                EstPubliee = a.EstPubliee,
                DatePublication = a.DatePublication,
                NomAuteur = a.Auteur != null ? a.Auteur.Prenom + " " + a.Auteur.Nom : null
            })
            .ToListAsync();

        dto.DernieresActivites = await db.ParticipantsActivite
            .Where(p => scoutIds.Contains(p.ScoutId) && !p.Activite.EstSupprime)
            .OrderByDescending(p => p.Activite.DateDebut)
            .GroupBy(p => new
            {
                p.Activite.Id,
                p.Activite.Titre,
                p.Activite.Description,
                p.Activite.DateDebut,
                p.Activite.DateFin,
                p.Activite.Lieu,
                p.Activite.Statut,
                NomGroupe = p.Activite.Groupe != null ? p.Activite.Groupe.Nom : null
            })
            .Select(g => new ActiviteDto
            {
                Id = g.Key.Id,
                Titre = g.Key.Titre,
                Description = g.Key.Description,
                DateDebut = g.Key.DateDebut,
                DateFin = g.Key.DateFin,
                Lieu = g.Key.Lieu,
                Statut = g.Key.Statut,
                NomGroupe = g.Key.NomGroupe
            })
            .OrderByDescending(a => a.DateDebut)
            .Take(5)
            .ToListAsync();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

}
