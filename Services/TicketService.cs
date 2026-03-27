using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class TicketService(AppDbContext db, IHubContext<NotificationHub> hubContext) : ITicketService
{
    public async Task<List<TicketDto>> GetAllAsync(
        StatutTicket? statut = null,
        TypeTicket? type = null,
        CategorieTicket? categorie = null,
        PrioriteTicket? priorite = null,
        string? vue = null,
        string? recherche = null,
        Guid? agentId = null)
    {
        await ApplyEscalationRulesAsync();

        var query = db.Tickets
            .Include(t => t.Createur)
            .Include(t => t.AssigneA)
            .Include(t => t.GroupeAssigne)
            .Include(t => t.ServiceCatalogue)
            .Include(t => t.Messages)
            .Include(t => t.PiecesJointes).ThenInclude(p => p.AjoutePar)
            .Where(t => !t.EstSupprime)
            .AsQueryable();

        if (statut.HasValue)
        {
            query = query.Where(t => t.Statut == statut.Value);
        }
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }
        if (categorie.HasValue)
        {
            query = query.Where(t => t.Categorie == categorie.Value);
        }
        if (priorite.HasValue)
        {
            query = query.Where(t => t.Priorite == priorite.Value);
        }
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            if (db.Database.IsNpgsql())
            {
                query = ApplySearchFilter(query, recherche);
            }
            else
            {
                var filteredTickets = await query.ToListAsync();
                var normalizedTerm = DatabaseText.NormalizeSearchKey(recherche);
                var filteredDtos = filteredTickets
                    .Where(t =>
                        DatabaseText.ContainsNormalized(t.Sujet, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(t.Description, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(t.NumeroTicket, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(t.Createur.Nom, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(t.Createur.Prenom, normalizedTerm))
                    .OrderByDescending(t => t.Priorite)
                    .ThenBy(t => t.DateLimiteSla)
                    .ThenBy(t => t.DateCreation)
                    .Select(ToDto)
                    .ToList();
                return ApplyQueueView(filteredDtos, vue, agentId);
            }
        }

        var tickets = await query
            .OrderByDescending(t => t.Priorite)
            .ThenBy(t => t.DateLimiteSla)
            .ThenBy(t => t.DateCreation)
            .ToListAsync();

        var mapped = tickets.Select(ToDto).ToList();
        return ApplyQueueView(mapped, vue, agentId);
    }

    public async Task<TicketDto?> GetByIdAsync(Guid id)
    {
        await ApplyEscalationRulesAsync();

        var ticket = await db.Tickets
            .Include(t => t.Createur)
            .Include(t => t.AssigneA)
            .Include(t => t.GroupeAssigne)
            .Include(t => t.ServiceCatalogue)
            .Include(t => t.Messages).ThenInclude(m => m.Auteur)
            .Include(t => t.Historiques).ThenInclude(h => h.Auteur)
            .Include(t => t.PiecesJointes).ThenInclude(p => p.AjoutePar)
            .FirstOrDefaultAsync(t => t.Id == id);
        return ticket is null ? null : ToDto(ticket);
    }

    public async Task<List<TicketDto>> GetByUserAsync(Guid userId)
    {
        await ApplyEscalationRulesAsync();

        var tickets = await db.Tickets
            .Include(t => t.Createur)
            .Include(t => t.AssigneA)
            .Include(t => t.GroupeAssigne)
            .Include(t => t.Messages)
            .Include(t => t.ServiceCatalogue)
            .Include(t => t.PiecesJointes).ThenInclude(p => p.AjoutePar)
            .Where(t => t.CreateurId == userId && !t.EstSupprime)
            .OrderByDescending(t => t.DateCreation)
            .ToListAsync();

        return tickets.Select(ToDto).ToList();
    }

    public async Task<TicketDto> CreateAsync(TicketCreateDto dto, Guid createurId)
    {
        var dateCreation = DateTime.UtcNow;
        var ticketId = Guid.NewGuid();
        var service = dto.ServiceCatalogueId.HasValue
            ? await db.SupportCatalogueServices
                .Include(s => s.AssigneParDefaut)
                .FirstOrDefaultAsync(s => s.Id == dto.ServiceCatalogueId.Value && s.EstActif)
            : null;
        var type = service?.TypeParDefaut ?? dto.Type;
        var categorie = service?.CategorieParDefaut ?? dto.Categorie;
        var impact = service?.ImpactParDefaut ?? dto.Impact;
        var urgence = service?.UrgenceParDefaut ?? dto.Urgence;
        var priorite = ComputePriority(impact, urgence);
        var assignment = await ResolveAutomaticAssignmentAsync(service);
        var hasAutomaticAssignment = assignment.AgentId.HasValue || assignment.GroupeId.HasValue;

        var ticket = new Ticket
        {
            Id = ticketId,
            NumeroTicket = GenerateTicketNumber(dateCreation, ticketId),
            Sujet = dto.Sujet,
            Description = dto.Description,
            Type = type,
            Categorie = categorie,
            Impact = impact,
            Urgence = urgence,
            Priorite = priorite,
            CreateurId = createurId,
            Statut = hasAutomaticAssignment ? StatutTicket.Affecte : StatutTicket.Nouveau,
            DateCreation = dateCreation,
            DateLimiteSla = service is null ? ComputeSlaDeadline(dateCreation, priorite) : dateCreation.AddHours(service.DelaiSlaHeures),
            ServiceCatalogueId = service?.Id,
            AssigneAId = assignment.AgentId,
            GroupeAssigneId = assignment.GroupeId,
            DateAffectation = hasAutomaticAssignment ? dateCreation : null
        };
        db.Tickets.Add(ticket);

        if (hasAutomaticAssignment)
        {
            db.HistoriquesTicket.Add(new HistoriqueTicket
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                AncienStatut = StatutTicket.Nouveau,
                NouveauStatut = StatutTicket.Affecte,
                AuteurId = null,
                Commentaire = assignment.HistoryComment,
                DateChangement = dateCreation
            });
        }

        await db.SaveChangesAsync();
        return (await GetByIdAsync(ticket.Id))!;
    }

    public async Task<bool> UpdateStatutAsync(Guid id, StatutTicket statut, Guid? auteurId = null)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return false;
        }

        var ancien = ticket.Statut;
        ticket.Statut = statut;

        if (statut == StatutTicket.Resolu || statut == StatutTicket.Ferme)
        {
            ticket.DateResolution ??= DateTime.UtcNow;
        }
        else if (statut == StatutTicket.Nouveau || statut == StatutTicket.Affecte || statut == StatutTicket.EnCours || statut == StatutTicket.EnAttente || statut == StatutTicket.EnAttenteDemandeur || statut == StatutTicket.EnAttenteTiers)
        {
            ticket.DateResolution = null;
        }

        db.HistoriquesTicket.Add(new HistoriqueTicket
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            AncienStatut = ancien,
            NouveauStatut = statut,
            AuteurId = auteurId,
            DateChangement = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignerAsync(Guid ticketId, Guid userId)
    {
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        var ancien = ticket.Statut;
        ticket.AssigneAId = userId;
        ticket.DateAffectation ??= DateTime.UtcNow;
        if (ticket.Statut == StatutTicket.Nouveau || ticket.Statut == StatutTicket.Ouvert)
        {
            ticket.Statut = StatutTicket.Affecte;
        }

        db.HistoriquesTicket.Add(new HistoriqueTicket
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AncienStatut = ancien,
            NouveauStatut = ticket.Statut,
            AuteurId = userId,
            Commentaire = "Ticket affecte a un agent.",
            DateChangement = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return true;
    }

    public Task<MessageTicketDto> AjouterMessageAsync(Guid ticketId, string contenu, Guid auteurId)
    {
        return AjouterMessageCoreAsync(ticketId, contenu, auteurId, false);
    }

    public Task<MessageTicketDto> AjouterNoteInterneAsync(Guid ticketId, string contenu, Guid auteurId)
    {
        return AjouterMessageCoreAsync(ticketId, contenu, auteurId, true);
    }

    public async Task<bool> NoterAsync(Guid ticketId, int note, string? commentaire)
    {
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        ticket.NoteSatisfaction = Math.Clamp(note, 1, 5);
        ticket.CommentaireSatisfaction = commentaire;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecategoriserAsync(Guid ticketId, TypeTicket type, CategorieTicket categorie, ImpactTicket impact, UrgenceTicket urgence)
    {
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        var priorite = ComputePriority(impact, urgence);
        ticket.Type = type;
        ticket.Categorie = categorie;
        ticket.Impact = impact;
        ticket.Urgence = urgence;
        ticket.Priorite = priorite;
        ticket.DateLimiteSla = ComputeSlaDeadline(ticket.DateCreation, priorite);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignerGroupeAsync(Guid ticketId, Guid groupeId)
    {
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        ticket.GroupeAssigneId = groupeId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResoudreAsync(Guid ticketId, string resumeResolution, bool fermerApresResolution, Guid auteurId)
    {
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return false;
        }

        var ancien = ticket.Statut;
        ticket.ResumeResolution = resumeResolution.Trim();
        ticket.DateResolution = DateTime.UtcNow;
        ticket.Statut = fermerApresResolution ? StatutTicket.Ferme : StatutTicket.Resolu;

        db.HistoriquesTicket.Add(new HistoriqueTicket
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AncienStatut = ancien,
            NouveauStatut = ticket.Statut,
            AuteurId = auteurId,
            Commentaire = "Resolution documentee.",
            DateChangement = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<SupportDashboardDto> GetSupportDashboardAsync(Guid? agentId = null)
    {
        await ApplyEscalationRulesAsync();

        var tickets = await db.Tickets
            .Include(t => t.Messages)
            .Include(t => t.AssigneA)
            .Where(t => !t.EstSupprime)
            .ToListAsync();

        var actifs = tickets.Where(t => !IsClosedStatus(t.Statut)).ToList();
        var resolus = tickets.Where(t => t.DateResolution.HasValue).ToList();
        var firstResponses = tickets
            .Where(t => t.DatePremiereReponse.HasValue)
            .Select(t => (t.DatePremiereReponse!.Value - t.DateCreation).TotalHours)
            .ToList();
        var notes = tickets
            .Where(t => t.NoteSatisfaction.HasValue)
            .Select(t => t.NoteSatisfaction!.Value)
            .ToList();

        var supportRoleNames = new[] { "Administrateur", "Gestionnaire", "AgentSupport" };
        var roleAgentIds = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => supportRoleNames.Contains(x.Name!))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        var supportUsers = await db.Users
            .Where(u => roleAgentIds.Contains(u.Id))
            .ToListAsync();

        var agentStats = supportUsers
            .Select(user =>
            {
                var assigned = tickets.Where(t => t.AssigneAId == user.Id).ToList();
                var assignedActive = assigned.Where(t => !IsClosedStatus(t.Statut)).ToList();
                var assignedResolved = assigned.Where(t => t.DateResolution.HasValue).ToList();
                var assignedFirstResponses = assigned
                    .Where(t => t.DatePremiereReponse.HasValue)
                    .Select(t => (t.DatePremiereReponse!.Value - t.DateCreation).TotalHours)
                    .ToList();
                var assignedNotes = assigned
                    .Where(t => t.NoteSatisfaction.HasValue)
                    .Select(t => t.NoteSatisfaction!.Value)
                    .ToList();

                return new SupportAgentStatDto
                {
                    AgentId = user.Id,
                    NomAgent = $"{user.Prenom} {user.Nom}",
                    TicketsActifs = assignedActive.Count,
                    TicketsResolus = assignedResolved.Count,
                    TicketsEnRetard = assignedActive.Count(IsOverdue),
                    TempsMoyenPremiereReponseHeures = assignedFirstResponses.Any() ? Math.Round(assignedFirstResponses.Average(), 1) : 0,
                    TempsMoyenResolutionHeures = assignedResolved.Any() ? Math.Round(assignedResolved.Average(t => (t.DateResolution!.Value - t.DateCreation).TotalHours), 1) : 0,
                    NoteMoyenneSatisfaction = assignedNotes.Any() ? Math.Round(assignedNotes.Average(), 1) : 0
                };
            })
            .OrderByDescending(a => a.TicketsActifs)
            .ThenByDescending(a => a.TicketsEnRetard)
            .ToList();

        return new SupportDashboardDto
        {
            TicketsOuverts = actifs.Count(t => t.Statut == StatutTicket.Nouveau || t.Statut == StatutTicket.Ouvert),
            TicketsEnCours = actifs.Count(t => t.Statut == StatutTicket.Affecte || t.Statut == StatutTicket.EnCours),
            TicketsEnAttente = actifs.Count(t => t.Statut == StatutTicket.EnAttente || t.Statut == StatutTicket.EnAttenteDemandeur || t.Statut == StatutTicket.EnAttenteTiers),
            TicketsNonAssignes = actifs.Count(t => !t.AssigneAId.HasValue),
            TicketsEnRetard = actifs.Count(IsOverdue),
            MesTicketsAssignes = agentId.HasValue ? actifs.Count(t => t.AssigneAId == agentId.Value) : 0,
            TicketsResolusAujourdHui = tickets.Count(t => (t.Statut == StatutTicket.Resolu || t.Statut == StatutTicket.Ferme) && t.DateResolution.HasValue && t.DateResolution.Value.Date == DateTime.Today),
            TempsMoyenResolutionHeures = resolus.Any() ? Math.Round(resolus.Average(t => (t.DateResolution!.Value - t.DateCreation).TotalHours), 1) : 0,
            TempsMoyenPremiereReponseHeures = firstResponses.Any() ? Math.Round(firstResponses.Average(), 1) : 0,
            NoteMoyenneSatisfaction = notes.Any() ? Math.Round(notes.Average(), 1) : 0,
            TotalTickets = tickets.Count,
            StatistiquesAgents = agentStats
        };
    }

    private async Task<MessageTicketDto> AjouterMessageCoreAsync(Guid ticketId, string contenu, Guid auteurId, bool isInternalNote)
    {
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            throw new InvalidOperationException("Ticket introuvable.");
        }

        var msg = new MessageTicket
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Contenu = contenu.Trim(),
            AuteurId = auteurId,
            EstNoteInterne = isInternalNote
        };
        db.MessagesTicket.Add(msg);

        if (!isInternalNote && auteurId != ticket.CreateurId && !ticket.DatePremiereReponse.HasValue)
        {
            ticket.DatePremiereReponse = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        var auteur = await db.Users.FindAsync(auteurId);
        return new MessageTicketDto
        {
            Id = msg.Id,
            Contenu = msg.Contenu,
            DateEnvoi = msg.DateEnvoi,
            AuteurId = msg.AuteurId,
            NomAuteur = auteur != null ? $"{auteur.Prenom} {auteur.Nom}" : "Inconnu",
            EstNoteInterne = isInternalNote
        };
    }

    private static List<TicketDto> ApplyQueueView(List<TicketDto> tickets, string? vue, Guid? agentId)
    {
        return (vue ?? "all").ToLowerInvariant() switch
        {
            "unassigned" => tickets.Where(t => !t.AssigneAId.HasValue && !IsClosedStatus(t.Statut)).ToList(),
            "mine" => agentId.HasValue ? tickets.Where(t => t.AssigneAId == agentId.Value && !IsClosedStatus(t.Statut)).ToList() : tickets,
            "overdue" => tickets.Where(t => t.EstEnRetard).ToList(),
            _ => tickets
        };
    }

    private IQueryable<Ticket> ApplySearchFilter(IQueryable<Ticket> query, string recherche)
    {
        var trimmedTerm = recherche.Trim();
        if (db.Database.IsNpgsql())
        {
            var pattern = DatabaseText.ToNormalizedContainsPattern(trimmedTerm);
            return query.Where(t =>
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(t.Sujet), pattern) ||
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(t.Description), pattern) ||
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(t.NumeroTicket), pattern) ||
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(t.Createur.Nom), pattern) ||
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(t.Createur.Prenom), pattern));
        }

        return query;
    }

    private TicketDto ToDto(Ticket ticket)
    {
        var deadline = ticket.DateLimiteSla == default ? ComputeSlaDeadline(ticket.DateCreation, ticket.Priorite) : ticket.DateLimiteSla;
        return new TicketDto
        {
            Id = ticket.Id,
            NumeroTicket = string.IsNullOrWhiteSpace(ticket.NumeroTicket) ? GenerateTicketNumber(ticket.DateCreation, ticket.Id) : ticket.NumeroTicket,
            ServiceCatalogueId = ticket.ServiceCatalogueId,
            NomServiceCatalogue = ticket.ServiceCatalogue?.Nom,
            Sujet = ticket.Sujet,
            Description = ticket.Description,
            Type = ticket.Type,
            Categorie = ticket.Categorie,
            Impact = ticket.Impact,
            Urgence = ticket.Urgence,
            Priorite = ticket.Priorite,
            Statut = ticket.Statut,
            DateCreation = ticket.DateCreation,
            DateResolution = ticket.DateResolution,
            DatePremiereReponse = ticket.DatePremiereReponse,
            DateLimiteSla = deadline,
            DateAffectation = ticket.DateAffectation,
            EstEnRetard = !IsClosedStatus(ticket.Statut) && DateTime.UtcNow > deadline,
            HeuresAvantSla = Math.Round((deadline - DateTime.UtcNow).TotalHours, 1),
            EstEscalade = ticket.EstEscalade,
            NiveauEscalade = ticket.NiveauEscalade,
            DateDerniereEscalade = ticket.DateDerniereEscalade,
            ResumeResolution = ticket.ResumeResolution,
            NoteSatisfaction = ticket.NoteSatisfaction,
            CommentaireSatisfaction = ticket.CommentaireSatisfaction,
            CreateurId = ticket.CreateurId,
            NomCreateur = ticket.Createur != null ? $"{ticket.Createur.Prenom} {ticket.Createur.Nom}" : null,
            AssigneAId = ticket.AssigneAId,
            NomAssigne = ticket.AssigneA != null ? $"{ticket.AssigneA.Prenom} {ticket.AssigneA.Nom}" : null,
            GroupeAssigneId = ticket.GroupeAssigneId,
            NomGroupeAssigne = ticket.GroupeAssigne?.Nom,
            Messages = ticket.Messages
                .OrderBy(m => m.DateEnvoi)
                .Select(m => new MessageTicketDto
                {
                    Id = m.Id,
                    Contenu = m.Contenu,
                    DateEnvoi = m.DateEnvoi,
                    AuteurId = m.AuteurId,
                    NomAuteur = m.Auteur != null ? $"{m.Auteur.Prenom} {m.Auteur.Nom}" : "Inconnu",
                    EstSupport = m.AuteurId != ticket.CreateurId,
                    EstNoteInterne = m.EstNoteInterne
                })
                .ToList(),
            PiecesJointes = ticket.PiecesJointes
                .OrderByDescending(p => p.DateAjout)
                .Select(p => new TicketAttachmentDto
                {
                    Id = p.Id,
                    NomOriginal = p.NomOriginal,
                    TypeMime = p.TypeMime,
                    TailleOctets = p.TailleOctets,
                    DateAjout = p.DateAjout,
                    NomAjoutePar = p.AjoutePar != null ? $"{p.AjoutePar.Prenom} {p.AjoutePar.Nom}" : "Inconnu"
                })
                .ToList(),
            Historiques = ticket.Historiques
                .OrderByDescending(h => h.DateChangement)
                .Select(h => new HistoriqueTicketDto
                {
                    AncienStatut = h.AncienStatut,
                    NouveauStatut = h.NouveauStatut,
                    NomAuteur = h.Auteur != null ? $"{h.Auteur.Prenom} {h.Auteur.Nom}" : null,
                    Commentaire = h.Commentaire,
                    DateChangement = h.DateChangement
                })
                .ToList()
        };
    }

    private async Task ApplyEscalationRulesAsync()
    {
        var now = DateTime.UtcNow;
        var activeTickets = await db.Tickets
            .Include(t => t.ServiceCatalogue)
            .Include(t => t.AssigneA)
            .Include(t => t.Createur)
            .Where(t => !t.EstSupprime)
            .Where(t => t.Statut != StatutTicket.Resolu && t.Statut != StatutTicket.Ferme && t.Statut != StatutTicket.Annule)
            .ToListAsync();

        if (!activeTickets.Any())
        {
            return;
        }

        var changes = false;

        foreach (var ticket in activeTickets)
        {
            var deadline = ticket.DateLimiteSla == default
                ? ComputeSlaDeadline(ticket.DateCreation, ticket.Priorite)
                : ticket.DateLimiteSla;
            var hoursRemaining = (deadline - now).TotalHours;
            var targetLevel = hoursRemaining <= 0 ? 2 : hoursRemaining <= GetEscalationThresholdHours(ticket.Priorite) ? 1 : 0;

            if (targetLevel == 0)
            {
                if (ticket.EstEscalade || ticket.NiveauEscalade != 0 || ticket.DateDerniereEscalade.HasValue)
                {
                    ticket.EstEscalade = false;
                    ticket.NiveauEscalade = 0;
                    ticket.DateDerniereEscalade = null;
                    changes = true;
                }

                continue;
            }

            if (targetLevel > ticket.NiveauEscalade)
            {
                ticket.EstEscalade = true;
                ticket.NiveauEscalade = targetLevel;
                ticket.DateDerniereEscalade = now;
                changes = true;

                var comment = targetLevel == 2
                    ? "Escalade automatique: SLA depasse."
                    : "Escalade automatique: SLA proche.";

                db.HistoriquesTicket.Add(new HistoriqueTicket
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    AncienStatut = ticket.Statut,
                    NouveauStatut = ticket.Statut,
                    AuteurId = null,
                    Commentaire = comment,
                    DateChangement = now
                });

                await NotifyEscalationAsync(ticket, targetLevel, comment);
            }

            if (targetLevel == 2 && (!ticket.AssigneAId.HasValue || !IsUserOperational(ticket.AssigneA)))
            {
                var reassignment = await ResolveAutomaticAssignmentAsync(ticket.ServiceCatalogue);
                if (reassignment.AgentId != ticket.AssigneAId || reassignment.GroupeId != ticket.GroupeAssigneId)
                {
                    ticket.AssigneAId = reassignment.AgentId;
                    ticket.GroupeAssigneId = reassignment.GroupeId;
                    ticket.DateAffectation = now;
                    if (ticket.Statut == StatutTicket.Nouveau || ticket.Statut == StatutTicket.Ouvert)
                    {
                        ticket.Statut = StatutTicket.Affecte;
                    }

                    db.HistoriquesTicket.Add(new HistoriqueTicket
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticket.Id,
                        AncienStatut = ticket.Statut,
                        NouveauStatut = ticket.Statut,
                        AuteurId = null,
                        Commentaire = $"Reaffectation automatique suite a escalade. {reassignment.HistoryComment}",
                        DateChangement = now
                    });
                    await NotifyReassignmentAsync(ticket, reassignment);
                    changes = true;
                }
            }
        }

        if (changes)
        {
            await db.SaveChangesAsync();
        }
    }

    private async Task<AutoAssignmentResult> ResolveAutomaticAssignmentAsync(SupportServiceCatalogueItem? service)
    {
        if (service is null)
        {
            var fallbackAgentId = await GetLeastLoadedSupportAgentAsync();
            return new AutoAssignmentResult(
                fallbackAgentId,
                null,
                fallbackAgentId.HasValue
                    ? "Affectation automatique a un agent support disponible."
                    : "Aucune affectation automatique disponible.");
        }

        if (service.AssigneParDefautId.HasValue)
        {
            var defaultAgent = service.AssigneParDefaut ?? await db.Users.FirstOrDefaultAsync(u => u.Id == service.AssigneParDefautId.Value);
            if (IsUserOperational(defaultAgent))
            {
                return new AutoAssignmentResult(
                    service.AssigneParDefautId,
                    service.GroupeParDefautId ?? defaultAgent?.GroupeId,
                    $"Affectation automatique via le service {service.Nom}.");
            }
        }

        if (service.GroupeParDefautId.HasValue)
        {
            var groupAgentId = await GetLeastLoadedSupportAgentAsync(service.GroupeParDefautId.Value);
            if (groupAgentId.HasValue)
            {
                return new AutoAssignmentResult(
                    groupAgentId,
                    service.GroupeParDefautId,
                    $"Affectation automatique intelligente via la file {service.GroupeParDefaut?.Nom ?? "support"}.");
            }
        }

        var globalAgentId = await GetLeastLoadedSupportAgentAsync();
        if (globalAgentId.HasValue)
        {
            return new AutoAssignmentResult(
                globalAgentId,
                service.GroupeParDefautId,
                $"Affectation automatique intelligente via un agent support disponible pour le service {service.Nom}.");
        }

        return new AutoAssignmentResult(
            null,
            service.GroupeParDefautId,
            service.GroupeParDefautId.HasValue
                ? $"Affectation automatique a la file {service.GroupeParDefaut?.Nom ?? "support"} en attente d'un agent."
                : "Aucune affectation automatique disponible.");
    }

    private async Task<Guid?> GetLeastLoadedSupportAgentAsync(Guid? groupeId = null)
    {
        var supportRoleNames = new[] { "Administrateur", "Gestionnaire", "AgentSupport" };
        var candidateIds = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => supportRoleNames.Contains(x.Name!))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        var usersQuery = db.Users
            .Where(u => candidateIds.Contains(u.Id))
            .Where(u => u.IsActive)
            .Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow);

        if (groupeId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.GroupeId == groupeId.Value);
        }

        var agents = await usersQuery
            .Select(u => new { u.Id })
            .ToListAsync();

        if (!agents.Any() && groupeId.HasValue)
        {
            return null;
        }

        if (!agents.Any())
        {
            return null;
        }

        var agentIds = agents.Select(a => a.Id).ToList();
        var workloads = await db.Tickets
            .Where(t => t.AssigneAId.HasValue && agentIds.Contains(t.AssigneAId.Value) && !t.EstSupprime)
            .Where(t => t.Statut != StatutTicket.Resolu && t.Statut != StatutTicket.Ferme && t.Statut != StatutTicket.Annule)
            .GroupBy(t => t.AssigneAId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count(), Oldest = g.Min(t => t.DateCreation) })
            .ToListAsync();

        return agents
            .Select(agent =>
            {
                var load = workloads.FirstOrDefault(w => w.AgentId == agent.Id);
                return new
                {
                    agent.Id,
                    Count = load?.Count ?? 0,
                    Oldest = load?.Oldest ?? DateTime.MinValue
                };
            })
            .OrderBy(x => x.Count)
            .ThenBy(x => x.Oldest)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();
    }

    private static bool IsUserOperational(ApplicationUser? user)
    {
        return user is not null
            && user.IsActive
            && (!user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow);
    }

    private static double GetEscalationThresholdHours(PrioriteTicket priorite)
    {
        return priorite switch
        {
            PrioriteTicket.Urgente => 1,
            PrioriteTicket.Haute => 2,
            PrioriteTicket.Normale => 6,
            _ => 18
        };
    }

    private async Task NotifyEscalationAsync(Ticket ticket, int targetLevel, string reason)
    {
        var recipients = new HashSet<Guid>();

        if (ticket.AssigneAId.HasValue)
        {
            recipients.Add(ticket.AssigneAId.Value);
        }

        foreach (var id in await GetSupervisionRecipientIdsAsync())
        {
            recipients.Add(id);
        }

        var severity = targetLevel == 2 ? "critique" : "proche";
        var title = targetLevel == 2 ? "Escalade SLA critique" : "Escalade SLA proche";
        var message = $"Ticket {ticket.NumeroTicket} en escalade {severity}: {ticket.Sujet}. {reason}";
        QueueNotifications(recipients, title, message, $"/Tickets/Details/{ticket.Id}");
        await NotifyUsersAsync(recipients, message);
    }

    private async Task NotifyReassignmentAsync(Ticket ticket, AutoAssignmentResult reassignment)
    {
        var recipients = new HashSet<Guid>();

        if (reassignment.AgentId.HasValue)
        {
            recipients.Add(reassignment.AgentId.Value);
        }

        foreach (var id in await GetSupervisionRecipientIdsAsync())
        {
            recipients.Add(id);
        }

        var destination = reassignment.AgentId.HasValue
            ? "vers un agent disponible"
            : "vers une file de support";
        var title = "Reaffectation automatique";
        var message = $"Ticket {ticket.NumeroTicket} reaffecte automatiquement {destination}: {ticket.Sujet}.";
        QueueNotifications(recipients, title, message, $"/Tickets/Details/{ticket.Id}");
        await NotifyUsersAsync(recipients, message);
    }

    private async Task<List<Guid>> GetSupervisionRecipientIdsAsync()
    {
        var supportRoleNames = new[] { "Administrateur", "Gestionnaire", "Superviseur" };
        return await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => supportRoleNames.Contains(x.Name!))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();
    }

    private void QueueNotifications(IEnumerable<Guid> recipients, string title, string message, string? link)
    {
        var userIds = recipients.Distinct().ToList();
        if (!userIds.Any())
        {
            return;
        }

        db.NotificationsUtilisateur.AddRange(userIds.Select(userId => new NotificationUtilisateur
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Titre = title,
            Message = message,
            Categorie = "Support",
            Lien = link,
            EstLue = false,
            DateCreation = DateTime.UtcNow
        }));
    }

    private async Task NotifyUsersAsync(IEnumerable<Guid> recipients, string message)
    {
        var userIds = recipients.Select(id => id.ToString()).Distinct().ToList();
        if (!userIds.Any())
        {
            return;
        }

        foreach (var userId in userIds)
        {
            await hubContext.Clients.User(userId).SendAsync("RecevoirNotification", message);
        }
    }

    private static string GenerateTicketNumber(DateTime dateCreation, Guid? id = null)
    {
        var suffix = (id ?? Guid.NewGuid()).ToString("N")[..6].ToUpperInvariant();
        return $"INC-{dateCreation:yyyyMMdd}-{suffix}";
    }

    private static DateTime ComputeSlaDeadline(DateTime createdAt, PrioriteTicket priorite)
    {
        var hours = priorite switch
        {
            PrioriteTicket.Urgente => 4,
            PrioriteTicket.Haute => 8,
            PrioriteTicket.Normale => 24,
            _ => 72
        };
        return createdAt.AddHours(hours);
    }

    private static bool IsClosedStatus(StatutTicket statut)
    {
        return statut == StatutTicket.Resolu || statut == StatutTicket.Ferme || statut == StatutTicket.Annule;
    }

    private static bool IsOverdue(Ticket ticket)
    {
        var deadline = ticket.DateLimiteSla == default ? ComputeSlaDeadline(ticket.DateCreation, ticket.Priorite) : ticket.DateLimiteSla;
        return !IsClosedStatus(ticket.Statut) && DateTime.UtcNow > deadline;
    }

    private static PrioriteTicket ComputePriority(ImpactTicket impact, UrgenceTicket urgence)
    {
        var score = (impact, urgence) switch
        {
            (ImpactTicket.Eleve, UrgenceTicket.Critique) => 4,
            (ImpactTicket.Eleve, UrgenceTicket.Haute) => 4,
            (ImpactTicket.Eleve, UrgenceTicket.Moyenne) => 3,
            (ImpactTicket.Moyen, UrgenceTicket.Critique) => 4,
            (ImpactTicket.Moyen, UrgenceTicket.Haute) => 3,
            (ImpactTicket.Moyen, UrgenceTicket.Moyenne) => 2,
            (ImpactTicket.Faible, UrgenceTicket.Critique) => 3,
            (ImpactTicket.Faible, UrgenceTicket.Haute) => 2,
            _ => 1
        };

        return score switch
        {
            4 => PrioriteTicket.Urgente,
            3 => PrioriteTicket.Haute,
            2 => PrioriteTicket.Normale,
            _ => PrioriteTicket.Basse
        };
    }

    private sealed record AutoAssignmentResult(Guid? AgentId, Guid? GroupeId, string HistoryComment);
}
