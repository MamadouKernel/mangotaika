using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MangoTaika.Services;

public class FormationService(AppDbContext db) : IFormationService
{
    private static readonly Expression<Func<Formation, FormationDto>> FormationDtoProjection = formation => new FormationDto
    {
        Id = formation.Id,
        Titre = formation.Titre,
        Description = formation.Description,
        ImageUrl = formation.ImageUrl,
        Niveau = formation.Niveau,
        Statut = formation.Statut,
        DureeEstimeeHeures = formation.DureeEstimeeHeures,
        DateCreation = formation.DateCreation,
        DelivreBadge = formation.DelivreBadge,
        DelivreAttestation = formation.DelivreAttestation,
        DelivreCertificat = formation.DelivreCertificat,
        NomBrancheCible = formation.BrancheCible != null ? formation.BrancheCible.Nom : null,
        BrancheCibleId = formation.BrancheCibleId,
        CompetenceLieeId = formation.CompetenceLieeId,
        NomAuteur = formation.Auteur.Prenom + " " + formation.Auteur.Nom,
        NombreModules = formation.Modules.Count(),
        NombreInscrits = formation.Inscriptions.Count()
    };

    public Task<int> CountAsync()
    {
        return db.Formations.AsNoTracking().CountAsync();
    }

    public async Task<List<FormationDto>> GetAllAsync()
    {
        var formations = await db.Formations
            .AsNoTracking()
            .OrderByDescending(f => f.DateCreation)
            .Select(FormationDtoProjection)
            .ToListAsync();

        await EnrichFormationSummariesAsync(formations);
        return formations;
    }

    public async Task<List<FormationDto>> GetPageAsync(int skip, int take)
    {
        var formations = await db.Formations
            .AsNoTracking()
            .OrderByDescending(f => f.DateCreation)
            .Skip(skip)
            .Take(take)
            .Select(FormationDtoProjection)
            .ToListAsync();

        await EnrichFormationSummariesAsync(formations);
        return formations;
    }

    public async Task<FormationDetailDto?> GetDetailAsync(Guid id)
    {
        var formation = await db.Formations
            .AsNoTracking()
            .Include(f => f.Auteur)
            .Include(f => f.BrancheCible)
            .Include(f => f.Modules)
                .ThenInclude(m => m.Lecons)
            .Include(f => f.Modules)
                .ThenInclude(m => m.Quiz)
                    .ThenInclude(q => q!.Questions)
                        .ThenInclude(q => q.Reponses)
            .Include(f => f.Inscriptions)
            .Include(f => f.Sessions)
            .Include(f => f.Annonces)
                .ThenInclude(a => a.Auteur)
            .Include(f => f.Jalons)
            .Include(f => f.Prerequis)
                .ThenInclude(p => p.PrerequisFormation)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formation is null)
            return null;

        var detail = new FormationDetailDto
        {
            Id = formation.Id,
            Titre = formation.Titre,
            Description = formation.Description,
            ImageUrl = formation.ImageUrl,
            Niveau = formation.Niveau,
            Statut = formation.Statut,
            DureeEstimeeHeures = formation.DureeEstimeeHeures,
            DateCreation = formation.DateCreation,
            DelivreBadge = formation.DelivreBadge,
            DelivreAttestation = formation.DelivreAttestation,
            DelivreCertificat = formation.DelivreCertificat,
            NomBrancheCible = formation.BrancheCible?.Nom,
            BrancheCibleId = formation.BrancheCibleId,
            CompetenceLieeId = formation.CompetenceLieeId,
            NomAuteur = $"{formation.Auteur.Prenom} {formation.Auteur.Nom}",
            NombreModules = formation.Modules.Count,
            NombreInscrits = formation.Inscriptions.Count,
            Modules = formation.Modules
                .OrderBy(m => m.Ordre)
                .Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Titre = m.Titre,
                    Description = m.Description,
                    Ordre = m.Ordre,
                    NombreLecons = m.Lecons.Count,
                    AQuiz = m.Quiz != null,
                    Lecons = m.Lecons
                        .OrderBy(l => l.Ordre)
                        .Select(l => new LeconDto
                        {
                            Id = l.Id,
                            Titre = l.Titre,
                            Type = l.Type,
                            ContenuTexte = l.ContenuTexte,
                            VideoUrl = l.VideoUrl,
                            DocumentUrl = l.DocumentUrl,
                            Ordre = l.Ordre,
                            DureeMinutes = l.DureeMinutes
                        })
                        .ToList(),
                    Quiz = m.Quiz == null
                        ? null
                        : new QuizDto
                        {
                            Id = m.Quiz.Id,
                            Titre = m.Quiz.Titre,
                            NoteMinimale = m.Quiz.NoteMinimale,
                            NombreTentativesMax = m.Quiz.NombreTentativesMax,
                            DateOuvertureDisponibilite = m.Quiz.DateOuvertureDisponibilite,
                            DateFermetureDisponibilite = m.Quiz.DateFermetureDisponibilite,
                            Questions = m.Quiz.Questions
                                .OrderBy(q => q.Ordre)
                                .Select(q => new QuestionDto
                                {
                                    Id = q.Id,
                                    Enonce = q.Enonce,
                                    Ordre = q.Ordre,
                                    Reponses = q.Reponses
                                        .OrderBy(r => r.Ordre)
                                        .Select(r => new ReponseDto
                                        {
                                            Id = r.Id,
                                            Texte = r.Texte,
                                            EstCorrecte = r.EstCorrecte,
                                            Ordre = r.Ordre
                                        })
                                        .ToList()
                                })
                                .ToList()
                        }
                })
                .ToList(),
            Sessions = formation.Sessions
                .OrderByDescending(s => s.EstSelfPaced)
                .ThenBy(s => s.DateOuverture ?? DateTime.MaxValue)
                .Select(s => new SessionFormationDto
                {
                    Id = s.Id,
                    Titre = s.Titre,
                    Description = s.Description,
                    EstSelfPaced = s.EstSelfPaced,
                    EstPubliee = s.EstPubliee,
                    DateOuverture = s.DateOuverture,
                    DateFermeture = s.DateFermeture,
                    StatutAffichage = BuildSessionStatus(s)
                })
                .ToList(),
            Annonces = formation.Annonces
                .Where(a => a.EstPubliee)
                .OrderByDescending(a => a.DatePublication)
                .Select(a => new AnnonceFormationDto
                {
                    Id = a.Id,
                    Titre = a.Titre,
                    Contenu = a.Contenu,
                    EstPubliee = a.EstPubliee,
                    DatePublication = a.DatePublication,
                    NomAuteur = a.Auteur != null ? a.Auteur.Prenom + " " + a.Auteur.Nom : null
                })
                .ToList(),
            Jalons = formation.Jalons
                .Where(j => j.EstPublie)
                .OrderBy(j => j.DateJalon)
                .Select(j => new JalonFormationDto
                {
                    Id = j.Id,
                    Titre = j.Titre,
                    Description = j.Description,
                    DateJalon = j.DateJalon,
                    Type = j.Type,
                    EstPublie = j.EstPublie
                })
                .ToList(),
            Prerequis = formation.Prerequis
                .OrderBy(p => p.PrerequisFormation.Titre)
                .Select(p => new PrerequisFormationDto
                {
                    FormationId = p.PrerequisFormationId,
                    Titre = p.PrerequisFormation.Titre
                })
                .ToList()
        };

        ApplyFeaturedSession(detail, detail.Sessions);
        detail.NombreAnnoncesPubliees = detail.Annonces.Count;
        detail.NombreDiscussions = await db.DiscussionsFormation
            .AsNoTracking()
            .CountAsync(d => d.FormationId == id);
        detail.DateDerniereActiviteForum = await db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => d.FormationId == id)
            .MaxAsync(d => (DateTime?)d.DateDerniereActivite);
        detail.DiscussionsRecentes = await db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => d.FormationId == id)
            .OrderByDescending(d => d.DateDerniereActivite)
            .Take(3)
            .Select(d => new DiscussionFormationDto
            {
                Id = d.Id,
                FormationId = d.FormationId,
                FormationTitre = d.Formation.Titre,
                Titre = d.Titre,
                ContenuInitial = d.ContenuInitial,
                DateCreation = d.DateCreation,
                DateDerniereActivite = d.DateDerniereActivite,
                EstVerrouillee = d.EstVerrouillee,
                NomAuteur = d.Auteur.Prenom + " " + d.Auteur.Nom,
                NombreMessages = d.Messages.Count()
            })
            .ToListAsync();
        return detail;
    }

    public async Task<Formation> CreateAsync(FormationCreateDto dto, Guid auteurId)
    {
        var formation = new Formation
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre,
            Description = dto.Description,
            Niveau = dto.Niveau,
            DureeEstimeeHeures = dto.DureeEstimeeHeures,
            BrancheCibleId = dto.BrancheCibleId,
            CompetenceLieeId = dto.CompetenceLieeId,
            DelivreBadge = dto.DelivreBadge,
            DelivreAttestation = dto.DelivreAttestation,
            DelivreCertificat = dto.DelivreCertificat,
            DelivranceConfiguree = true,
            AuteurId = auteurId
        };

        db.Formations.Add(formation);
        await db.SaveChangesAsync();
        await SyncPrerequisitesAsync(formation.Id, dto.PrerequisFormationIds);
        return formation;
    }

    public async Task<bool> UpdateAsync(Guid id, FormationCreateDto dto)
    {
        var formation = await db.Formations.FindAsync(id);
        if (formation is null)
            return false;

        formation.Titre = dto.Titre;
        formation.Description = dto.Description;
        formation.Niveau = dto.Niveau;
        formation.DureeEstimeeHeures = dto.DureeEstimeeHeures;
        formation.BrancheCibleId = dto.BrancheCibleId;
        formation.CompetenceLieeId = dto.CompetenceLieeId;
        formation.DelivreBadge = dto.DelivreBadge;
        formation.DelivreAttestation = dto.DelivreAttestation;
        formation.DelivreCertificat = dto.DelivreCertificat;
        formation.DelivranceConfiguree = true;

        await db.SaveChangesAsync();
        await SyncPrerequisitesAsync(formation.Id, dto.PrerequisFormationIds);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var formation = await db.Formations.FindAsync(id);
        if (formation is null)
            return false;

        db.Formations.Remove(formation);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PublierAsync(Guid id)
    {
        var formation = await db.Formations.FindAsync(id);
        if (formation is null)
            return false;

        formation.Statut = StatutFormation.Publiee;
        formation.DatePublication = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiverAsync(Guid id)
    {
        var formation = await db.Formations.FindAsync(id);
        if (formation is null)
            return false;

        formation.Statut = StatutFormation.Archivee;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ModuleFormation> AjouterModuleAsync(Guid formationId, ModuleCreateDto dto)
    {
        var maxOrdre = await db.ModulesFormation
            .Where(m => m.FormationId == formationId)
            .MaxAsync(m => (int?)m.Ordre) ?? 0;

        var module = new ModuleFormation
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre,
            Description = dto.Description,
            Ordre = maxOrdre + 1,
            FormationId = formationId
        };

        db.ModulesFormation.Add(module);
        await db.SaveChangesAsync();
        return module;
    }

    public async Task<bool> UpdateModuleAsync(Guid moduleId, ModuleCreateDto dto)
    {
        var module = await db.ModulesFormation.FindAsync(moduleId);
        if (module is null)
            return false;

        module.Titre = dto.Titre;
        module.Description = dto.Description;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteModuleAsync(Guid moduleId)
    {
        var module = await db.ModulesFormation.FindAsync(moduleId);
        if (module is null)
            return false;

        db.ModulesFormation.Remove(module);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Lecon> AjouterLeconAsync(Guid moduleId, LeconCreateDto dto)
    {
        var maxOrdre = await db.Lecons
            .Where(l => l.ModuleId == moduleId)
            .MaxAsync(l => (int?)l.Ordre) ?? 0;

        var lecon = new Lecon
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre,
            Type = dto.Type,
            ContenuTexte = dto.ContenuTexte,
            VideoUrl = dto.VideoUrl,
            DureeMinutes = dto.DureeMinutes,
            Ordre = maxOrdre + 1,
            ModuleId = moduleId
        };

        db.Lecons.Add(lecon);
        await db.SaveChangesAsync();
        return lecon;
    }

    public async Task<bool> UpdateLeconAsync(Guid leconId, LeconCreateDto dto)
    {
        var lecon = await db.Lecons.FindAsync(leconId);
        if (lecon is null)
            return false;

        lecon.Titre = dto.Titre;
        lecon.Type = dto.Type;
        lecon.ContenuTexte = dto.ContenuTexte;
        lecon.VideoUrl = dto.VideoUrl;
        lecon.DureeMinutes = dto.DureeMinutes;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLeconAsync(Guid leconId)
    {
        var lecon = await db.Lecons.FindAsync(leconId);
        if (lecon is null)
            return false;

        db.Lecons.Remove(lecon);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Quiz> CreerQuizAsync(
        Guid moduleId,
        string titre,
        int noteMinimale,
        int? nombreTentativesMax = null,
        DateTime? dateOuvertureDisponibilite = null,
        DateTime? dateFermetureDisponibilite = null)
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Titre = titre,
            NoteMinimale = noteMinimale,
            NombreTentativesMax = nombreTentativesMax,
            DateOuvertureDisponibilite = dateOuvertureDisponibilite,
            DateFermetureDisponibilite = dateFermetureDisponibilite,
            ModuleId = moduleId
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> UpdateQuizAsync(
        Guid quizId,
        string titre,
        int noteMinimale,
        int? nombreTentativesMax,
        DateTime? dateOuvertureDisponibilite,
        DateTime? dateFermetureDisponibilite)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz is null)
            return false;

        quiz.Titre = titre.Trim();
        quiz.NoteMinimale = noteMinimale;
        quiz.NombreTentativesMax = nombreTentativesMax;
        quiz.DateOuvertureDisponibilite = dateOuvertureDisponibilite;
        quiz.DateFermetureDisponibilite = dateFermetureDisponibilite;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task AjouterQuestionAsync(Guid quizId, QuestionCreateDto dto)
    {
        var maxOrdre = await db.QuestionsQuiz
            .Where(q => q.QuizId == quizId)
            .MaxAsync(q => (int?)q.Ordre) ?? 0;

        var question = new QuestionQuiz
        {
            Id = Guid.NewGuid(),
            Enonce = dto.Enonce,
            Ordre = maxOrdre + 1,
            QuizId = quizId,
            Reponses = dto.Reponses
                .Select((r, index) => new ReponseQuiz
                {
                    Id = Guid.NewGuid(),
                    Texte = r.Texte,
                    EstCorrecte = r.EstCorrecte,
                    Ordre = index + 1
                })
                .ToList()
        };

        db.QuestionsQuiz.Add(question);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        var question = await db.QuestionsQuiz.FindAsync(questionId);
        if (question is null)
            return false;

        db.QuestionsQuiz.Remove(question);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuizAsync(Guid quizId)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz is null)
            return false;

        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<SessionFormation> AjouterSessionAsync(Guid formationId, SessionFormationCreateDto dto)
    {
        var session = new SessionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = formationId,
            Titre = dto.Titre,
            Description = dto.Description,
            EstSelfPaced = dto.EstSelfPaced,
            EstPubliee = dto.EstPubliee,
            DateOuverture = dto.EstSelfPaced ? null : dto.DateOuverture,
            DateFermeture = dto.EstSelfPaced ? null : dto.DateFermeture
        };

        db.SessionsFormation.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        var session = await db.SessionsFormation.FindAsync(sessionId);
        if (session is null)
            return false;

        db.SessionsFormation.Remove(session);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<AnnonceFormation> AjouterAnnonceAsync(Guid formationId, AnnonceFormationCreateDto dto, Guid? auteurId)
    {
        var annonce = new AnnonceFormation
        {
            Id = Guid.NewGuid(),
            FormationId = formationId,
            Titre = dto.Titre,
            Contenu = dto.Contenu,
            EstPubliee = dto.EstPubliee,
            AuteurId = auteurId,
            DatePublication = DateTime.UtcNow
        };

        db.AnnoncesFormation.Add(annonce);
        await db.SaveChangesAsync();
        return annonce;
    }

    public async Task<bool> DeleteAnnonceAsync(Guid annonceId)
    {
        var annonce = await db.AnnoncesFormation.FindAsync(annonceId);
        if (annonce is null)
            return false;

        db.AnnoncesFormation.Remove(annonce);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<JalonFormation> AjouterJalonAsync(Guid formationId, JalonFormationCreateDto dto)
    {
        var jalon = new JalonFormation
        {
            Id = Guid.NewGuid(),
            FormationId = formationId,
            Titre = dto.Titre.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            DateJalon = dto.DateJalon,
            Type = dto.Type,
            EstPublie = dto.EstPublie
        };

        db.JalonsFormation.Add(jalon);
        await db.SaveChangesAsync();
        return jalon;
    }

    public async Task<bool> DeleteJalonAsync(Guid jalonId)
    {
        var jalon = await db.JalonsFormation.FindAsync(jalonId);
        if (jalon is null)
            return false;

        db.JalonsFormation.Remove(jalon);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<List<DiscussionFormationDto>> GetDiscussionsAsync(Guid formationId)
    {
        return db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => d.FormationId == formationId)
            .OrderByDescending(d => d.DateDerniereActivite)
            .ThenByDescending(d => d.DateCreation)
            .Select(d => new DiscussionFormationDto
            {
                Id = d.Id,
                FormationId = d.FormationId,
                FormationTitre = d.Formation.Titre,
                Titre = d.Titre,
                ContenuInitial = d.ContenuInitial,
                DateCreation = d.DateCreation,
                DateDerniereActivite = d.DateDerniereActivite,
                EstVerrouillee = d.EstVerrouillee,
                NomAuteur = d.Auteur.Prenom + " " + d.Auteur.Nom,
                NombreMessages = d.Messages.Count()
            })
            .ToListAsync();
    }

    public async Task<DiscussionFormationDetailDto?> GetDiscussionAsync(Guid discussionId)
    {
        var discussion = await db.DiscussionsFormation
            .AsNoTracking()
            .Include(d => d.Formation)
            .Include(d => d.Auteur)
            .Include(d => d.Messages.Where(m => !m.EstSupprime).OrderBy(m => m.DateCreation))
                .ThenInclude(m => m.Auteur)
            .FirstOrDefaultAsync(d => d.Id == discussionId);

        if (discussion is null)
            return null;

        return new DiscussionFormationDetailDto
        {
            Id = discussion.Id,
            FormationId = discussion.FormationId,
            FormationTitre = discussion.Formation.Titre,
            Titre = discussion.Titre,
            ContenuInitial = discussion.ContenuInitial,
            DateCreation = discussion.DateCreation,
            DateDerniereActivite = discussion.DateDerniereActivite,
            EstVerrouillee = discussion.EstVerrouillee,
            NomAuteur = discussion.Auteur.Prenom + " " + discussion.Auteur.Nom,
            NombreMessages = discussion.Messages.Count,
            Messages = discussion.Messages
                .OrderBy(m => m.DateCreation)
                .Select(m => new MessageDiscussionFormationDto
                {
                    Id = m.Id,
                    Contenu = m.Contenu,
                    DateCreation = m.DateCreation,
                    NomAuteur = m.Auteur.Prenom + " " + m.Auteur.Nom
                })
                .ToList()
        };
    }

    public async Task<DiscussionFormation> AjouterDiscussionAsync(Guid formationId, Guid auteurId, DiscussionFormationCreateDto dto)
    {
        var discussion = new DiscussionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = formationId,
            AuteurId = auteurId,
            Titre = dto.Titre.Trim(),
            ContenuInitial = dto.Contenu.Trim(),
            DateCreation = DateTime.UtcNow,
            DateDerniereActivite = DateTime.UtcNow
        };

        db.DiscussionsFormation.Add(discussion);
        await db.SaveChangesAsync();
        return discussion;
    }

    public async Task<MessageDiscussionFormation> AjouterMessageDiscussionAsync(Guid discussionId, Guid auteurId, MessageDiscussionFormationCreateDto dto)
    {
        var discussion = await db.DiscussionsFormation.FindAsync(discussionId)
            ?? throw new InvalidOperationException("Discussion introuvable");

        if (discussion.EstVerrouillee)
            throw new InvalidOperationException("Cette discussion est verrouillee.");

        var message = new MessageDiscussionFormation
        {
            Id = Guid.NewGuid(),
            DiscussionFormationId = discussionId,
            AuteurId = auteurId,
            Contenu = dto.Contenu.Trim(),
            DateCreation = DateTime.UtcNow
        };

        discussion.DateDerniereActivite = message.DateCreation;
        db.MessagesDiscussionFormation.Add(message);
        await db.SaveChangesAsync();
        return message;
    }

    public async Task<bool> BasculerVerrouDiscussionAsync(Guid discussionId)
    {
        var discussion = await db.DiscussionsFormation.FindAsync(discussionId);
        if (discussion is null)
            return false;

        discussion.EstVerrouillee = !discussion.EstVerrouillee;
        discussion.DateDerniereActivite = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<FormationDto>> GetCatalogueAsync(Guid? brancheId, Guid? scoutId = null)
    {
        IQueryable<Formation> query = db.Formations
            .AsNoTracking()
            .Where(f => f.Statut == StatutFormation.Publiee);

        if (brancheId.HasValue)
            query = query.Where(f => f.BrancheCibleId == null || f.BrancheCibleId == brancheId);

        var formations = await query
            .OrderByDescending(f => f.DatePublication)
            .Select(FormationDtoProjection)
            .ToListAsync();

        await EnrichFormationSummariesAsync(formations, scoutId);
        return formations;
    }

    public async Task<InscriptionFormation> InscrireScoutAsync(Guid formationId, Guid scoutId)
    {
        var existing = await db.InscriptionsFormation
            .FirstOrDefaultAsync(i => i.ScoutId == scoutId && i.FormationId == formationId);

        if (existing != null)
            return existing;

        var prerequisBloquants = await GetUnmetPrerequisitesAsync(formationId, scoutId);
        if (prerequisBloquants.Count != 0)
            throw new InvalidOperationException($"Inscription impossible. Terminez d'abord : {string.Join(", ", prerequisBloquants)}.");

        var inscription = new InscriptionFormation
        {
            Id = Guid.NewGuid(),
            ScoutId = scoutId,
            FormationId = formationId,
            SessionFormationId = await ResolvePreferredSessionIdAsync(formationId)
        };

        db.InscriptionsFormation.Add(inscription);
        await db.SaveChangesAsync();
        return inscription;
    }

    public Task<bool> EstInscritAsync(Guid formationId, Guid scoutId)
    {
        return db.InscriptionsFormation.AnyAsync(i => i.FormationId == formationId && i.ScoutId == scoutId);
    }

    public async Task<List<LmsParcoursItemDto>> GetParcoursScoutsAsync(IEnumerable<Guid> scoutIds)
    {
        var scoutIdList = scoutIds
            .Distinct()
            .ToList();

        if (scoutIdList.Count == 0)
            return [];

        var inscriptions = await db.InscriptionsFormation
            .AsNoTracking()
            .Where(i => scoutIdList.Contains(i.ScoutId))
            .Include(i => i.Formation)
            .Include(i => i.SessionFormation)
            .Include(i => i.Scout)
            .OrderByDescending(i => i.DateInscription)
            .ToListAsync();

        if (inscriptions.Count == 0)
            return [];

        var formationIds = inscriptions
            .Select(i => i.FormationId)
            .Distinct()
            .ToList();

        var modules = await db.ModulesFormation
            .AsNoTracking()
            .Where(m => formationIds.Contains(m.FormationId))
            .Select(m => new
            {
                m.Id,
                m.FormationId,
                NombreLecons = m.Lecons.Count(),
                AQuiz = m.Quiz != null
            })
            .ToListAsync();

        var publishedSessions = await db.SessionsFormation
            .AsNoTracking()
            .Where(s => formationIds.Contains(s.FormationId) && s.EstPubliee)
            .OrderByDescending(s => s.EstSelfPaced)
            .ThenBy(s => s.DateOuverture ?? DateTime.MaxValue)
            .Select(s => new
            {
                s.FormationId,
                Session = new SessionFormationDto
                {
                    Id = s.Id,
                    Titre = s.Titre,
                    Description = s.Description,
                    EstSelfPaced = s.EstSelfPaced,
                    EstPubliee = s.EstPubliee,
                    DateOuverture = s.DateOuverture,
                    DateFermeture = s.DateFermeture,
                    StatutAffichage = BuildSessionStatus(s)
                }
            })
            .ToListAsync();

        var leconsTerminees = await db.ProgressionsLecon
            .AsNoTracking()
            .Where(p => scoutIdList.Contains(p.ScoutId) && p.EstTerminee && formationIds.Contains(p.Lecon.Module.FormationId))
            .Select(p => new
            {
                p.ScoutId,
                ModuleId = p.Lecon.ModuleId
            })
            .ToListAsync();

        var quizProgressions = await db.TentativesQuiz
            .AsNoTracking()
            .Where(t => scoutIdList.Contains(t.ScoutId) && formationIds.Contains(t.Quiz.Module.FormationId))
            .GroupBy(t => new { t.ScoutId, ModuleId = t.Quiz.ModuleId })
            .Select(g => new
            {
                g.Key.ScoutId,
                g.Key.ModuleId,
                Reussi = g.Any(t => t.Reussi),
                MeilleurScore = g.Max(t => (int?)t.Score)
            })
            .ToListAsync();

        var certifications = await db.CertificationsFormation
            .AsNoTracking()
            .Where(c => scoutIdList.Contains(c.ScoutId) && formationIds.Contains(c.FormationId))
            .Select(c => new { c.ScoutId, c.FormationId, c.Type })
            .ToListAsync();

        var annonces = await db.AnnoncesFormation
            .AsNoTracking()
            .Where(a => a.EstPubliee && formationIds.Contains(a.FormationId))
            .GroupBy(a => a.FormationId)
            .Select(g => new
            {
                FormationId = g.Key,
                Count = g.Count(),
                LastActivity = g.Max(a => (DateTime?)a.DatePublication)
            })
            .ToListAsync();

        var discussions = await db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => formationIds.Contains(d.FormationId))
            .GroupBy(d => d.FormationId)
            .Select(g => new
            {
                FormationId = g.Key,
                Count = g.Count(),
                LastActivity = g.Max(d => (DateTime?)d.DateDerniereActivite)
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var prochainsJalons = await db.JalonsFormation
            .AsNoTracking()
            .Where(j => formationIds.Contains(j.FormationId) && j.EstPublie && j.DateJalon >= now)
            .GroupBy(j => j.FormationId)
            .Select(g => new
            {
                FormationId = g.Key,
                ProchainJalonDate = g.Min(j => (DateTime?)j.DateJalon),
                ProchainJalonTitre = g.OrderBy(j => j.DateJalon).Select(j => j.Titre).FirstOrDefault()
            })
            .ToListAsync();

        var modulesByFormation = modules
            .GroupBy(m => m.FormationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var featuredSessionByFormation = publishedSessions
            .GroupBy(s => s.FormationId)
            .ToDictionary(g => g.Key, g => SelectFeaturedSession(g.Select(x => x.Session).ToList()));

        var leconsParScoutModule = leconsTerminees
            .GroupBy(x => (x.ScoutId, x.ModuleId))
            .ToDictionary(g => g.Key, g => g.Count());

        var quizParScoutModule = quizProgressions
            .ToDictionary(
                x => (x.ScoutId, x.ModuleId),
                x => new QuizProgressSnapshot(x.Reussi, x.MeilleurScore));

        var certificationsParScoutFormation = certifications
            .GroupBy(x => (x.ScoutId, x.FormationId))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Type).ToHashSet());

        var annoncesParFormation = annonces
            .ToDictionary(x => x.FormationId, x => new ActivitySnapshot(x.Count, x.LastActivity));

        var discussionsParFormation = discussions
            .ToDictionary(x => x.FormationId, x => new ActivitySnapshot(x.Count, x.LastActivity));

        var jalonsParFormation = prochainsJalons.ToDictionary(
            x => x.FormationId,
            x => new { x.ProchainJalonDate, x.ProchainJalonTitre });

        var parcours = new List<LmsParcoursItemDto>(inscriptions.Count);
        foreach (var inscription in inscriptions)
        {
            modulesByFormation.TryGetValue(inscription.FormationId, out var moduleItems);
            moduleItems ??= [];

            var quizTotal = moduleItems.Count(m => m.AQuiz);
            var quizReussis = 0;
            int? meilleurScoreQuiz = null;
            var modulesTermines = 0;

            foreach (var module in moduleItems)
            {
                var leconsCompletes = leconsParScoutModule.GetValueOrDefault((inscription.ScoutId, module.Id));
                var quizState = quizParScoutModule.GetValueOrDefault((inscription.ScoutId, module.Id));
                if (quizState?.MeilleurScore is int moduleScore)
                    meilleurScoreQuiz = Math.Max(meilleurScoreQuiz ?? int.MinValue, moduleScore);

                if (module.AQuiz && quizState?.Reussi == true)
                    quizReussis++;

                if (leconsCompletes >= module.NombreLecons && (!module.AQuiz || quizState?.Reussi == true))
                    modulesTermines++;
            }

            if (meilleurScoreQuiz == int.MinValue)
                meilleurScoreQuiz = null;

            var certTypes = certificationsParScoutFormation.GetValueOrDefault((inscription.ScoutId, inscription.FormationId)) ?? [];
            var badgeObtenu = certTypes.Contains(TypeCertificationFormation.Badge);
            var attestationObtenue = certTypes.Contains(TypeCertificationFormation.Attestation);
            var certificatObtenu = certTypes.Contains(TypeCertificationFormation.Certificat);
            var annonceSummary = annoncesParFormation.GetValueOrDefault(inscription.FormationId);
            var discussionSummary = discussionsParFormation.GetValueOrDefault(inscription.FormationId);
            var sessionCourante = inscription.SessionFormation != null
                ? new SessionFormationDto
                {
                    Id = inscription.SessionFormation.Id,
                    Titre = inscription.SessionFormation.Titre,
                    Description = inscription.SessionFormation.Description,
                    EstSelfPaced = inscription.SessionFormation.EstSelfPaced,
                    EstPubliee = inscription.SessionFormation.EstPubliee,
                    DateOuverture = inscription.SessionFormation.DateOuverture,
                    DateFermeture = inscription.SessionFormation.DateFermeture,
                    StatutAffichage = BuildSessionStatus(inscription.SessionFormation)
                }
                : featuredSessionByFormation.GetValueOrDefault(inscription.FormationId);

            parcours.Add(new LmsParcoursItemDto
            {
                FormationId = inscription.FormationId,
                ScoutId = inscription.ScoutId,
                Titre = inscription.Formation.Titre,
                Description = inscription.Formation.Description,
                NomScout = inscription.Scout.Prenom + " " + inscription.Scout.Nom,
                SessionTitre = sessionCourante?.Titre,
                SessionStatut = sessionCourante?.StatutAffichage,
                EstSessionSelfPaced = sessionCourante?.EstSelfPaced ?? false,
                DateOuvertureSession = sessionCourante?.DateOuverture,
                DateFermetureSession = sessionCourante?.DateFermeture,
                ProgressionPourcent = inscription.ProgressionPourcent,
                Statut = inscription.Statut,
                NombreModules = moduleItems.Count,
                NombreModulesTermines = modulesTermines,
                NombreQuiz = quizTotal,
                NombreQuizReussis = quizReussis,
                MeilleurScoreQuiz = meilleurScoreQuiz,
                NombreAnnonces = annonceSummary?.Count ?? 0,
                NombreDiscussions = discussionSummary?.Count ?? 0,
                DerniereActiviteCours = MaxDate(annonceSummary?.LastActivity, discussionSummary?.LastActivity),
                ProchainJalonDate = jalonsParFormation.GetValueOrDefault(inscription.FormationId)?.ProchainJalonDate,
                ProchainJalonTitre = jalonsParFormation.GetValueOrDefault(inscription.FormationId)?.ProchainJalonTitre,
                DelivreBadge = inscription.Formation.DelivreBadge,
                DelivreAttestation = inscription.Formation.DelivreAttestation,
                DelivreCertificat = inscription.Formation.DelivreCertificat,
                BadgeObtenu = badgeObtenu,
                AttestationObtenue = attestationObtenue,
                CertificatObtenu = certificatObtenu,
                NombreCertificationsObtenues = certTypes.Count,
                EtatPedagogique = BuildPedagogicalStatus(
                    inscription.Statut,
                    inscription.ProgressionPourcent,
                    sessionCourante?.EstSelfPaced ?? false,
                    sessionCourante?.DateOuverture,
                    sessionCourante?.DateFermeture),
                EtatEvaluation = BuildEvaluationStatus(quizTotal, quizReussis, meilleurScoreQuiz),
                EtatCertifiant = BuildCertificationStatus(
                    inscription.Formation.DelivreBadge,
                    inscription.Formation.DelivreAttestation,
                    inscription.Formation.DelivreCertificat,
                    badgeObtenu,
                    attestationObtenue,
                    certificatObtenu,
                    inscription.Statut),
                ProchaineEtape = BuildNextStep(
                    inscription.ProgressionPourcent,
                    quizTotal,
                    quizReussis,
                    inscription.Statut,
                    certTypes.Count > 0,
                    sessionCourante?.EstSelfPaced ?? false,
                    sessionCourante?.DateOuverture)
            });
        }

        return parcours;
    }

    public Task<bool> LeconAppartientFormationAsync(Guid leconId, Guid formationId)
    {
        return db.Lecons.AnyAsync(l => l.Id == leconId && l.Module.FormationId == formationId);
    }

    public Task<bool> QuizAppartientFormationAsync(Guid quizId, Guid formationId)
    {
        return db.Quizzes.AnyAsync(q => q.Id == quizId && q.Module.FormationId == formationId);
    }

    public async Task<QuizPassagePageDto?> GetQuizPassageAsync(Guid quizId, Guid formationId, Guid scoutId)
    {
        if (!await EstInscritAsync(formationId, scoutId))
            return null;

        var quizAccess = await GetQuizAccessDecisionAsync(quizId, formationId, scoutId);
        if (quizAccess is null)
            return null;

        var quiz = await db.Quizzes
            .AsNoTracking()
            .Where(q => q.Id == quizId && q.Module.FormationId == formationId)
            .Select(q => new QuizDto
            {
                Id = q.Id,
                Titre = q.Titre,
                NoteMinimale = q.NoteMinimale,
                NombreTentativesMax = q.NombreTentativesMax,
                DateOuvertureDisponibilite = q.DateOuvertureDisponibilite,
                DateFermetureDisponibilite = q.DateFermetureDisponibilite,
                Questions = q.Questions
                    .OrderBy(question => question.Ordre)
                    .Select(question => new QuestionDto
                    {
                        Id = question.Id,
                        Enonce = question.Enonce,
                        Ordre = question.Ordre,
                        Reponses = question.Reponses
                            .OrderBy(response => response.Ordre)
                            .Select(response => new ReponseDto
                            {
                                Id = response.Id,
                                Texte = response.Texte,
                                EstCorrecte = response.EstCorrecte,
                                Ordre = response.Ordre
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (quiz is null)
            return null;

        var tentatives = await db.TentativesQuiz
            .AsNoTracking()
            .Where(t => t.ScoutId == scoutId && t.QuizId == quizId)
            .OrderByDescending(t => t.DateTentative)
            .Select(t => new QuizTentativeDto
            {
                Id = t.Id,
                Score = t.Score,
                Reussi = t.Reussi,
                DateTentative = t.DateTentative
            })
            .ToListAsync();

        var meilleurScore = tentatives.Count > 0 ? (int?)tentatives.Max(t => t.Score) : null;
        var quizValide = tentatives.Any(t => t.Reussi);

        return new QuizPassagePageDto
        {
            FormationId = formationId,
            Quiz = quiz,
            Tentatives = tentatives,
            MeilleurScore = meilleurScore,
            NombreTentatives = tentatives.Count,
            NombreTentativesRestantes = quiz.NombreTentativesMax.HasValue
                ? Math.Max(quiz.NombreTentativesMax.Value - tentatives.Count, 0)
                : null,
            EtatEvaluation = BuildEvaluationStatus(1, quizValide ? 1 : 0, meilleurScore),
            PeutSoumettre = quizAccess.PeutInteragir,
            EstLectureSeule = !quizAccess.PeutInteragir,
            MessageAcces = quizAccess.Message
        };
    }

    public async Task<FormationProgressionDto?> GetProgressionAsync(Guid formationId, Guid scoutId)
    {
        if (!await EstInscritAsync(formationId, scoutId))
            return null;

        var inscription = await db.InscriptionsFormation
            .AsNoTracking()
            .Include(i => i.SessionFormation)
            .FirstOrDefaultAsync(i => i.FormationId == formationId && i.ScoutId == scoutId);

        if (inscription is null)
            return null;

        var detail = await GetDetailAsync(formationId);
        if (detail is null)
            return null;

        var effectiveSession = inscription.SessionFormation != null
            ? new SessionFormationDto
            {
                Id = inscription.SessionFormation.Id,
                Titre = inscription.SessionFormation.Titre,
                Description = inscription.SessionFormation.Description,
                EstSelfPaced = inscription.SessionFormation.EstSelfPaced,
                EstPubliee = inscription.SessionFormation.EstPubliee,
                DateOuverture = inscription.SessionFormation.DateOuverture,
                DateFermeture = inscription.SessionFormation.DateFermeture,
                StatutAffichage = BuildSessionStatus(inscription.SessionFormation)
            }
            : detail.Sessions.FirstOrDefault(s => s.Id == detail.SessionId);

        if (effectiveSession != null)
        {
            detail.SessionId = effectiveSession.Id;
            detail.SessionTitre = effectiveSession.Titre;
            detail.SessionStatut = effectiveSession.StatutAffichage;
            detail.EstSessionSelfPaced = effectiveSession.EstSelfPaced;
            detail.DateOuvertureSession = effectiveSession.DateOuverture;
            detail.DateFermetureSession = effectiveSession.DateFermeture;
        }

        var sessionAccess = BuildSessionAccessSnapshot(
            effectiveSession?.EstSelfPaced ?? false,
            effectiveSession?.DateOuverture,
            effectiveSession?.DateFermeture);

        var progressions = await db.ProgressionsLecon
            .Where(p => p.ScoutId == scoutId && p.Lecon.Module.FormationId == formationId)
            .ToListAsync();

        var tentatives = await db.TentativesQuiz
            .Where(t => t.ScoutId == scoutId && t.Quiz.Module.FormationId == formationId)
            .ToListAsync();

        var totalLecons = detail.Modules.Sum(m => m.Lecons.Count);
        var leconsTerminees = progressions.Count(p => p.EstTerminee);
        var leconsTermineesIds = progressions
            .Where(p => p.EstTerminee)
            .Select(p => p.LeconId)
            .ToHashSet();
        var quizReussisIds = tentatives
            .Where(t => t.Reussi)
            .Select(t => t.QuizId)
            .ToHashSet();

        var modulesProgression = new List<ModuleProgressionDto>(detail.Modules.Count);
        var previousModulesCompleted = true;
        foreach (var module in detail.Modules.OrderBy(m => m.Ordre))
        {
            var orderedLessons = module.Lecons
                .OrderBy(l => l.Ordre)
                .ToList();
            var terminees = orderedLessons.Count(l => leconsTermineesIds.Contains(l.Id));
            var tentativesModule = module.Quiz != null
                ? tentatives
                    .Where(t => t.QuizId == module.Quiz.Id)
                    .OrderByDescending(t => t.DateTentative)
                    .ToList()
                : [];
            var quizReussi = module.Quiz != null && tentativesModule.Any(t => t.Reussi);
            var meilleurScore = module.Quiz != null
                ? tentativesModule.MaxBy(t => t.Score)?.Score
                : null;
            var availability = BuildModuleAvailabilitySnapshot(
                module,
                orderedLessons,
                leconsTermineesIds,
                previousModulesCompleted,
                sessionAccess);
            var moduleComplete = IsModuleComplete(terminees, orderedLessons.Count, module.AQuiz, quizReussi);

            modulesProgression.Add(new ModuleProgressionDto
            {
                Module = module,
                LeconsTerminees = terminees,
                TotalLecons = orderedLessons.Count,
                QuizReussi = quizReussi,
                MeilleurScore = meilleurScore,
                NombreTentativesQuiz = tentativesModule.Count,
                TentativesRestantesQuiz = module.Quiz?.NombreTentativesMax.HasValue == true
                    ? Math.Max(module.Quiz.NombreTentativesMax.Value - tentativesModule.Count, 0)
                    : null,
                DateDerniereTentativeQuiz = tentativesModule.FirstOrDefault()?.DateTentative,
                TentativesQuiz = tentativesModule
                    .Select(t => new QuizTentativeDto
                    {
                        Id = t.Id,
                        Score = t.Score,
                        Reussi = t.Reussi,
                        DateTentative = t.DateTentative
                    })
                    .ToList(),
                EstDisponible = availability.EstDisponible,
                MessageBlocage = availability.MessageBlocage,
                LeconsDisponiblesIds = availability.LeconsDisponiblesIds,
                QuizDisponible = availability.QuizDisponible,
                MessageQuiz = availability.MessageQuiz
            });

            previousModulesCompleted = previousModulesCompleted && moduleComplete;
        }

        var certifications = await db.CertificationsFormation
            .AsNoTracking()
            .Where(c => c.ScoutId == scoutId && c.FormationId == formationId)
            .Select(c => c.Type)
            .ToListAsync();

        var quizTotal = modulesProgression.Count(m => m.Module.AQuiz);
        var quizReussis = modulesProgression.Count(m => m.QuizReussi);
        var meilleurScoreGlobal = modulesProgression
            .Where(m => m.MeilleurScore.HasValue)
            .Select(m => m.MeilleurScore)
            .DefaultIfEmpty()
            .Max();
        var statutCourant = modulesProgression.All(m => m.LeconsTerminees == m.TotalLecons && (!m.Module.AQuiz || m.QuizReussi))
            ? StatutInscription.Terminee
            : StatutInscription.EnCours;
        var prochaineEtape = BuildNextStep(
            totalLecons > 0 ? (int)(leconsTerminees * 100.0 / totalLecons) : 0,
            quizTotal,
            quizReussis,
            statutCourant,
            certifications.Count > 0,
            effectiveSession?.EstSelfPaced ?? false,
            effectiveSession?.DateOuverture);

        if (!sessionAccess.PeutInteragir && !string.IsNullOrWhiteSpace(sessionAccess.Message))
        {
            prochaineEtape = sessionAccess.Message!;
        }
        else
        {
            var prochainModule = modulesProgression.FirstOrDefault(m => !IsModuleComplete(m.LeconsTerminees, m.TotalLecons, m.Module.AQuiz, m.QuizReussi));
            if (prochainModule != null)
            {
                var prochaineLecon = prochainModule.Module.Lecons
                    .OrderBy(l => l.Ordre)
                    .FirstOrDefault(l => prochainModule.LeconsDisponiblesIds.Contains(l.Id) && !leconsTermineesIds.Contains(l.Id));

                if (prochaineLecon != null)
                {
                    prochaineEtape = $"Suivre la lecon {prochaineLecon.Ordre} : {prochaineLecon.Titre}";
                }
                else if (prochainModule.Module.AQuiz && !prochainModule.QuizReussi && prochainModule.QuizDisponible)
                {
                    prochaineEtape = $"Passer le quiz du module {prochainModule.Module.Ordre}";
                }
            }
        }

        var notificationLink = $"/Formations/Suivre/{formationId}";
        var notificationsLms = await db.NotificationsUtilisateur
            .AsNoTracking()
            .Where(n => n.UserId == scoutId && n.Categorie == "LMS" && n.Lien == notificationLink)
            .OrderByDescending(n => n.DateCreation)
            .Take(5)
            .Select(n => new NotificationLmsDto
            {
                Id = n.Id,
                Titre = n.Titre,
                Message = n.Message,
                Categorie = n.Categorie,
                Lien = n.Lien,
                EstLue = n.EstLue,
                DateCreation = n.DateCreation
            })
            .ToListAsync();

        return new FormationProgressionDto
        {
            Formation = detail,
            PourcentageGlobal = totalLecons > 0 ? (int)(leconsTerminees * 100.0 / totalLecons) : 0,
            Modules = modulesProgression,
            LeconsTermineesIds = leconsTermineesIds,
            NombreModulesTermines = modulesProgression.Count(m => m.LeconsTerminees == m.TotalLecons && (!m.Module.AQuiz || m.QuizReussi)),
            NombreModulesTotal = detail.Modules.Count,
            NombreQuizReussis = quizReussis,
            NombreQuizTotal = quizTotal,
            MeilleurScoreGlobal = meilleurScoreGlobal,
            BadgeObtenu = certifications.Contains(TypeCertificationFormation.Badge),
            AttestationObtenue = certifications.Contains(TypeCertificationFormation.Attestation),
            CertificatObtenu = certifications.Contains(TypeCertificationFormation.Certificat),
            EtatPedagogique = BuildPedagogicalStatus(
                statutCourant,
                totalLecons > 0 ? (int)(leconsTerminees * 100.0 / totalLecons) : 0,
                effectiveSession?.EstSelfPaced ?? false,
                effectiveSession?.DateOuverture,
                effectiveSession?.DateFermeture),
            EtatEvaluation = BuildEvaluationStatus(quizTotal, quizReussis, meilleurScoreGlobal),
            EtatCertifiant = BuildCertificationStatus(
                detail.DelivreBadge,
                detail.DelivreAttestation,
                detail.DelivreCertificat,
                certifications.Contains(TypeCertificationFormation.Badge),
                certifications.Contains(TypeCertificationFormation.Attestation),
                certifications.Contains(TypeCertificationFormation.Certificat),
                statutCourant),
            ProchaineEtape = prochaineEtape,
            PeutInteragir = sessionAccess.PeutInteragir,
            EstLectureSeule = sessionAccess.EstLectureSeule,
            MessageAcces = sessionAccess.Message,
            NotificationsLms = notificationsLms
        };
    }

    public async Task MarquerLeconTermineeAsync(Guid leconId, Guid scoutId)
    {
        var formationId = await GetFormationIdForLeconAsync(leconId);
        if (!formationId.HasValue)
            throw new InvalidOperationException("Lecon introuvable");

        if (!await EstInscritAsync(formationId.Value, scoutId))
            throw new InvalidOperationException("Le scout n'est pas inscrit a cette formation");

        var lessonAccess = await GetLessonAccessDecisionAsync(leconId, formationId.Value, scoutId);
        if (lessonAccess is null)
            throw new InvalidOperationException("Lecon introuvable");
        if (!lessonAccess.PeutInteragir)
            throw new InvalidOperationException(lessonAccess.Message ?? "Cette lecon n'est pas encore accessible.");

        var existing = await db.ProgressionsLecon
            .FirstOrDefaultAsync(p => p.ScoutId == scoutId && p.LeconId == leconId);

        if (existing != null)
        {
            existing.EstTerminee = true;
            existing.DateTerminee = DateTime.UtcNow;
        }
        else
        {
            db.ProgressionsLecon.Add(new ProgressionLecon
            {
                Id = Guid.NewGuid(),
                ScoutId = scoutId,
                LeconId = leconId,
                EstTerminee = true,
                DateTerminee = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await MettreAJourProgressionAsync(formationId.Value, scoutId);
        await VerifierCompletionFormationAsync(leconId, scoutId);
    }

    public async Task<TentativeQuiz> SoumettreQuizAsync(Guid quizId, Guid scoutId, Dictionary<Guid, Guid> reponses)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Module)
            .Include(q => q.Questions)
                .ThenInclude(q => q.Reponses)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
            throw new InvalidOperationException("Quiz introuvable");

        if (!await EstInscritAsync(quiz.Module.FormationId, scoutId))
            throw new InvalidOperationException("Le scout n'est pas inscrit a cette formation");

        var quizAccess = await GetQuizAccessDecisionAsync(quizId, quiz.Module.FormationId, scoutId);
        if (quizAccess is null)
            throw new InvalidOperationException("Quiz introuvable");
        if (!quizAccess.PeutInteragir)
            throw new InvalidOperationException(quizAccess.Message ?? "Ce quiz n'est pas encore disponible.");

        var bonnes = 0;
        var total = quiz.Questions.Count;

        foreach (var question in quiz.Questions)
        {
            if (!reponses.TryGetValue(question.Id, out var reponseId))
                continue;

            var reponse = question.Reponses.FirstOrDefault(r => r.Id == reponseId);
            if (reponse?.EstCorrecte == true)
                bonnes++;
        }

        var score = total > 0 ? (int)(bonnes * 100.0 / total) : 0;
        var tentative = new TentativeQuiz
        {
            Id = Guid.NewGuid(),
            ScoutId = scoutId,
            QuizId = quizId,
            Score = score,
            Reussi = score >= quiz.NoteMinimale,
            ReponsesJson = System.Text.Json.JsonSerializer.Serialize(reponses)
        };

        db.TentativesQuiz.Add(tentative);
        await db.SaveChangesAsync();

        if (tentative.Reussi)
            await VerifierCompletionFormationAsync(quiz.ModuleId, scoutId);

        return tentative;
    }

    public async Task<List<InscriptionFormation>> GetInscriptionsScoutAsync(Guid scoutId)
    {
        return await db.InscriptionsFormation
            .AsNoTracking()
            .Include(i => i.Formation)
                .ThenInclude(f => f.Auteur)
            .Include(i => i.Formation)
                .ThenInclude(f => f.Modules)
            .Include(i => i.SessionFormation)
            .Where(i => i.ScoutId == scoutId)
            .OrderByDescending(i => i.DateInscription)
            .ToListAsync();
    }

    public async Task<FormationStatsDto> GetStatsAsync(Guid formationId)
    {
        var stats = await GetStatsByFormationAsync([formationId]);
        return stats.TryGetValue(formationId, out var value) ? value : new FormationStatsDto();
    }

    public async Task<Dictionary<Guid, FormationStatsDto>> GetStatsByFormationAsync(IEnumerable<Guid> formationIds)
    {
        var ids = formationIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var aggregates = await db.InscriptionsFormation
            .AsNoTracking()
            .Where(i => ids.Contains(i.FormationId))
            .GroupBy(i => i.FormationId)
            .Select(g => new
            {
                FormationId = g.Key,
                TotalInscrits = g.Count(),
                Terminees = g.Count(i => i.Statut == StatutInscription.Terminee),
                EnCours = g.Count(i => i.Statut == StatutInscription.EnCours)
            })
            .ToListAsync();

        var result = ids.ToDictionary(id => id, _ => new FormationStatsDto());
        foreach (var item in aggregates)
        {
            result[item.FormationId] = new FormationStatsDto
            {
                TotalInscrits = item.TotalInscrits,
                Terminees = item.Terminees,
                EnCours = item.EnCours,
                TauxReussite = item.TotalInscrits > 0
                    ? Math.Round(item.Terminees * 100.0 / item.TotalInscrits, 1)
                    : 0
            };
        }

        return result;
    }

    private async Task VerifierCompletionFormationAsync(Guid moduleOrLeconId, Guid scoutId)
    {
        var lecon = await db.Lecons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == moduleOrLeconId);

        Guid formationId;
        if (lecon != null)
        {
            formationId = lecon.Module.FormationId;
        }
        else
        {
            var module = await db.ModulesFormation.FindAsync(moduleOrLeconId);
            if (module is null)
                return;

            formationId = module.FormationId;
        }

        var formation = await db.Formations
            .Include(f => f.Modules)
                .ThenInclude(m => m.Lecons)
            .Include(f => f.Modules)
                .ThenInclude(m => m.Quiz)
            .FirstOrDefaultAsync(f => f.Id == formationId);

        if (formation is null)
            return;

        var allLeconIds = formation.Modules
            .SelectMany(m => m.Lecons)
            .Select(l => l.Id)
            .ToList();

        var progressions = await db.ProgressionsLecon
            .Where(p => p.ScoutId == scoutId && allLeconIds.Contains(p.LeconId) && p.EstTerminee)
            .CountAsync();

        if (progressions < allLeconIds.Count)
            return;

        var quizIds = formation.Modules
            .Where(m => m.Quiz != null)
            .Select(m => m.Quiz!.Id)
            .ToList();

        foreach (var quizId in quizIds)
        {
            var reussi = await db.TentativesQuiz
                .AnyAsync(t => t.ScoutId == scoutId && t.QuizId == quizId && t.Reussi);

            if (!reussi)
                return;
        }

        var inscription = await db.InscriptionsFormation
            .FirstOrDefaultAsync(i => i.ScoutId == scoutId && i.FormationId == formationId);

        if (inscription is null)
            return;

        if (inscription.Statut == StatutInscription.Terminee)
        {
            await EnsureCertificationsAsync(formation, inscription, scoutId);
            return;
        }

        inscription.Statut = StatutInscription.Terminee;
        inscription.DateTerminee = DateTime.UtcNow;
        inscription.ProgressionPourcent = 100;

        if (formation.CompetenceLieeId.HasValue)
        {
            var dejaCompetence = await db.Competences
                .AnyAsync(c => c.ScoutId == scoutId && c.Nom == formation.Titre);

            if (!dejaCompetence)
            {
                db.Competences.Add(new Competence
                {
                    Id = Guid.NewGuid(),
                    Nom = $"Formation : {formation.Titre}",
                    Description = $"Competence obtenue via la formation \"{formation.Titre}\"",
                    DateObtention = DateTime.UtcNow,
                    Niveau = formation.Niveau.ToString(),
                    Type = TypeCompetence.Scoute,
                    ScoutId = scoutId
                });
            }
        }

        await db.SaveChangesAsync();
        await EnsureCertificationsAsync(formation, inscription, scoutId);
    }

    private async Task MettreAJourProgressionAsync(Guid formationId, Guid scoutId)
    {
        var totalLecons = await db.Lecons.CountAsync(l => l.Module.FormationId == formationId);
        var terminees = await db.ProgressionsLecon
            .CountAsync(p => p.ScoutId == scoutId && p.Lecon.Module.FormationId == formationId && p.EstTerminee);

        var inscription = await db.InscriptionsFormation
            .FirstOrDefaultAsync(i => i.ScoutId == scoutId && i.FormationId == formationId);

        if (inscription is null)
            return;

        inscription.ProgressionPourcent = totalLecons > 0 ? (int)(terminees * 100.0 / totalLecons) : 0;
        await db.SaveChangesAsync();
    }

    private Task<Guid?> GetFormationIdForLeconAsync(Guid leconId)
    {
        return db.Lecons
            .Where(l => l.Id == leconId)
            .Select(l => (Guid?)l.Module.FormationId)
            .FirstOrDefaultAsync();
    }

    private async Task SyncPrerequisitesAsync(Guid formationId, IEnumerable<Guid>? prerequisFormationIds)
    {
        var requestedIds = prerequisFormationIds?
            .Where(id => id != Guid.Empty && id != formationId)
            .Distinct()
            .ToList() ?? [];

        var existing = await db.FormationsPrerequis
            .Where(p => p.FormationId == formationId)
            .ToListAsync();

        var toRemove = existing
            .Where(p => !requestedIds.Contains(p.PrerequisFormationId))
            .ToList();

        if (toRemove.Count != 0)
            db.FormationsPrerequis.RemoveRange(toRemove);

        var existingIds = existing.Select(p => p.PrerequisFormationId).ToHashSet();
        var validIds = await db.Formations
            .AsNoTracking()
            .Where(f => requestedIds.Contains(f.Id) && f.Id != formationId)
            .Select(f => f.Id)
            .ToListAsync();

        foreach (var prerequisId in validIds.Where(id => !existingIds.Contains(id)))
        {
            db.FormationsPrerequis.Add(new FormationPrerequis
            {
                FormationId = formationId,
                PrerequisFormationId = prerequisId
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<List<string>> GetUnmetPrerequisitesAsync(Guid formationId, Guid scoutId)
    {
        var prerequis = await db.FormationsPrerequis
            .AsNoTracking()
            .Where(p => p.FormationId == formationId)
            .Select(p => new
            {
                p.PrerequisFormationId,
                Titre = p.PrerequisFormation.Titre
            })
            .ToListAsync();

        if (prerequis.Count == 0)
            return [];

        var completedIds = await db.InscriptionsFormation
            .AsNoTracking()
            .Where(i => i.ScoutId == scoutId && i.Statut == StatutInscription.Terminee)
            .Select(i => i.FormationId)
            .ToHashSetAsync();

        return prerequis
            .Where(p => !completedIds.Contains(p.PrerequisFormationId))
            .Select(p => p.Titre)
            .ToList();
    }

    private async Task EnrichFormationSummariesAsync(List<FormationDto> formations, Guid? scoutId = null)
    {
        if (formations.Count == 0)
            return;

        var formationIds = formations.Select(f => f.Id).ToList();
        var sessions = await db.SessionsFormation
            .AsNoTracking()
            .Where(s => formationIds.Contains(s.FormationId) && s.EstPubliee)
            .OrderByDescending(s => s.EstSelfPaced)
            .ThenBy(s => s.DateOuverture ?? DateTime.MaxValue)
            .ToListAsync();

        var annonces = await db.AnnoncesFormation
            .AsNoTracking()
            .Where(a => formationIds.Contains(a.FormationId) && a.EstPubliee)
            .GroupBy(a => a.FormationId)
            .Select(g => new { FormationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.FormationId, g => g.Count);

        var discussions = await db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => formationIds.Contains(d.FormationId))
            .GroupBy(d => d.FormationId)
            .Select(g => new
            {
                FormationId = g.Key,
                Count = g.Count(),
                DateDerniereActivite = g.Max(d => (DateTime?)d.DateDerniereActivite)
            })
            .ToDictionaryAsync(
                g => g.FormationId,
                g => new { g.Count, g.DateDerniereActivite });

        var prochainsJalons = await db.JalonsFormation
            .AsNoTracking()
            .Where(j => formationIds.Contains(j.FormationId) && j.EstPublie && j.DateJalon >= DateTime.UtcNow)
            .GroupBy(j => j.FormationId)
            .Select(g => new
            {
                FormationId = g.Key,
                DateJalon = g.Min(j => (DateTime?)j.DateJalon),
                Titre = g.OrderBy(j => j.DateJalon).Select(j => j.Titre).FirstOrDefault()
            })
            .ToDictionaryAsync(g => g.FormationId, g => new { g.DateJalon, g.Titre });

        var prerequis = await db.FormationsPrerequis
            .AsNoTracking()
            .Where(p => formationIds.Contains(p.FormationId))
            .Select(p => new
            {
                p.FormationId,
                p.PrerequisFormationId,
                Titre = p.PrerequisFormation.Titre
            })
            .ToListAsync();

        var prerequisParFormation = prerequis
            .GroupBy(p => p.FormationId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Titre).ToList());

        HashSet<Guid> completedFormationIds = [];
        if (scoutId.HasValue)
        {
            completedFormationIds = await db.InscriptionsFormation
                .AsNoTracking()
                .Where(i => i.ScoutId == scoutId.Value && i.Statut == StatutInscription.Terminee)
                .Select(i => i.FormationId)
                .ToHashSetAsync();
        }

        foreach (var formation in formations)
        {
            var sessionDtos = sessions
                .Where(s => s.FormationId == formation.Id)
                .Select(s => new SessionFormationDto
                {
                    Id = s.Id,
                    Titre = s.Titre,
                    Description = s.Description,
                    EstSelfPaced = s.EstSelfPaced,
                    EstPubliee = s.EstPubliee,
                    DateOuverture = s.DateOuverture,
                    DateFermeture = s.DateFermeture,
                    StatutAffichage = BuildSessionStatus(s)
                })
                .ToList();

            ApplyFeaturedSession(formation, sessionDtos);
            formation.NombreAnnoncesPubliees = annonces.GetValueOrDefault(formation.Id);
            formation.ProchainJalonDate = prochainsJalons.GetValueOrDefault(formation.Id)?.DateJalon;
            formation.ProchainJalonTitre = prochainsJalons.GetValueOrDefault(formation.Id)?.Titre;
            if (discussions.TryGetValue(formation.Id, out var discussionSummary))
            {
                formation.NombreDiscussions = discussionSummary.Count;
                formation.DateDerniereActiviteForum = discussionSummary.DateDerniereActivite;
            }

            var prerequisItems = prerequisParFormation.GetValueOrDefault(formation.Id) ?? [];
            formation.Prerequis = prerequisItems
                .Select(item => new PrerequisFormationDto
                {
                    FormationId = item.PrerequisFormationId,
                    Titre = item.Titre,
                    EstValide = completedFormationIds.Contains(item.PrerequisFormationId)
                })
                .ToList();
            formation.NombrePrerequisRestants = formation.Prerequis.Count(p => !p.EstValide);
            formation.PeutSInscrire = formation.NombrePrerequisRestants == 0;
            if (formation.NombrePrerequisRestants > 0)
            {
                formation.MessageInscription = "Terminez d'abord les prerequis requis pour debloquer cette formation.";
            }
        }
    }

    private static void ApplyFeaturedSession(FormationDto formation, IReadOnlyList<SessionFormationDto> sessions)
    {
        var featured = SelectFeaturedSession(sessions);
        if (featured is null)
            return;

        formation.SessionId = featured.Id;
        formation.SessionTitre = featured.Titre;
        formation.SessionStatut = featured.StatutAffichage;
        formation.EstSessionSelfPaced = featured.EstSelfPaced;
        formation.DateOuvertureSession = featured.DateOuverture;
        formation.DateFermetureSession = featured.DateFermeture;
    }

    private static SessionFormationDto? SelectFeaturedSession(IReadOnlyList<SessionFormationDto> sessions)
    {
        if (sessions.Count == 0)
            return null;

        var now = DateTime.UtcNow;

        return sessions.FirstOrDefault(s => s.EstSelfPaced)
            ?? sessions.FirstOrDefault(s => s.DateOuverture.HasValue && s.DateFermeture.HasValue && s.DateOuverture.Value <= now && s.DateFermeture.Value >= now)
            ?? sessions.FirstOrDefault(s => s.DateOuverture.HasValue && s.DateOuverture.Value >= now)
            ?? sessions.OrderByDescending(s => s.DateFermeture ?? DateTime.MinValue).FirstOrDefault();
    }

    private static SessionFormationDto? SelectEnrollmentSession(IReadOnlyList<SessionFormationDto> sessions)
    {
        if (sessions.Count == 0)
            return null;

        var now = DateTime.UtcNow;

        return sessions.FirstOrDefault(s => s.EstSelfPaced)
            ?? sessions.FirstOrDefault(s =>
                s.DateOuverture.HasValue &&
                (!s.DateFermeture.HasValue || s.DateFermeture.Value >= now) &&
                s.DateOuverture.Value <= now)
            ?? sessions.FirstOrDefault(s => s.DateOuverture.HasValue && s.DateOuverture.Value >= now);
    }

    private async Task<Guid?> ResolvePreferredSessionIdAsync(Guid formationId)
    {
        var sessions = await db.SessionsFormation
            .AsNoTracking()
            .Where(s => s.FormationId == formationId && s.EstPubliee)
            .OrderByDescending(s => s.EstSelfPaced)
            .ThenBy(s => s.DateOuverture ?? DateTime.MaxValue)
            .Select(s => new SessionFormationDto
            {
                Id = s.Id,
                Titre = s.Titre,
                EstSelfPaced = s.EstSelfPaced,
                DateOuverture = s.DateOuverture,
                DateFermeture = s.DateFermeture,
                StatutAffichage = string.Empty
            })
            .ToListAsync();

        return SelectEnrollmentSession(sessions)?.Id;
    }

    private async Task<LessonAccessDecision?> GetLessonAccessDecisionAsync(Guid leconId, Guid formationId, Guid scoutId)
    {
        var snapshot = await BuildLearningAccessSnapshotAsync(formationId, scoutId);
        if (snapshot is null)
            return null;

        var previousModulesCompleted = true;
        foreach (var module in snapshot.Detail.Modules.OrderBy(m => m.Ordre))
        {
            var orderedLessons = module.Lecons
                .OrderBy(l => l.Ordre)
                .ToList();
            var availability = BuildModuleAvailabilitySnapshot(
                module,
                orderedLessons,
                snapshot.LeconsTermineesIds,
                previousModulesCompleted,
                snapshot.SessionAccess);
            var lesson = orderedLessons.FirstOrDefault(l => l.Id == leconId);
            if (lesson != null)
            {
                if (snapshot.LeconsTermineesIds.Contains(leconId))
                    return new LessonAccessDecision(true, null);

                if (availability.EstDisponible && availability.LeconsDisponiblesIds.Contains(leconId))
                    return new LessonAccessDecision(true, null);

                return new LessonAccessDecision(false, availability.MessageBlocage ?? "Terminez la ressource precedente pour debloquer cette lecon.");
            }

            previousModulesCompleted = previousModulesCompleted && IsModuleComplete(
                orderedLessons.Count(l => snapshot.LeconsTermineesIds.Contains(l.Id)),
                orderedLessons.Count,
                module.AQuiz,
                module.Quiz != null && snapshot.QuizzesReussisIds.Contains(module.Quiz.Id));
        }

        return null;
    }

    private async Task<QuizAccessDecision?> GetQuizAccessDecisionAsync(Guid quizId, Guid formationId, Guid scoutId)
    {
        var snapshot = await BuildLearningAccessSnapshotAsync(formationId, scoutId);
        if (snapshot is null)
            return null;

        var tentativesQuiz = await db.TentativesQuiz
            .AsNoTracking()
            .Where(t => t.ScoutId == scoutId && t.QuizId == quizId)
            .CountAsync();

        var previousModulesCompleted = true;
        foreach (var module in snapshot.Detail.Modules.OrderBy(m => m.Ordre))
        {
            var orderedLessons = module.Lecons
                .OrderBy(l => l.Ordre)
                .ToList();
            var availability = BuildModuleAvailabilitySnapshot(
                module,
                orderedLessons,
                snapshot.LeconsTermineesIds,
                previousModulesCompleted,
                snapshot.SessionAccess,
                module.Quiz?.Id == quizId ? tentativesQuiz : 0);

            if (module.Quiz?.Id == quizId)
            {
                return new QuizAccessDecision(
                    availability.QuizDisponible,
                    availability.QuizDisponible ? null : availability.MessageQuiz ?? "Terminez d'abord les lecons du module pour lancer cette evaluation.");
            }

            previousModulesCompleted = previousModulesCompleted && IsModuleComplete(
                orderedLessons.Count(l => snapshot.LeconsTermineesIds.Contains(l.Id)),
                orderedLessons.Count,
                module.AQuiz,
                module.Quiz != null && snapshot.QuizzesReussisIds.Contains(module.Quiz.Id));
        }

        return null;
    }

    private async Task<LearningAccessSnapshot?> BuildLearningAccessSnapshotAsync(Guid formationId, Guid scoutId)
    {
        var inscription = await db.InscriptionsFormation
            .AsNoTracking()
            .Include(i => i.SessionFormation)
            .FirstOrDefaultAsync(i => i.FormationId == formationId && i.ScoutId == scoutId);

        if (inscription is null)
            return null;

        var detail = await GetDetailAsync(formationId);
        if (detail is null)
            return null;

        var effectiveSession = inscription.SessionFormation != null
            ? new SessionFormationDto
            {
                Id = inscription.SessionFormation.Id,
                Titre = inscription.SessionFormation.Titre,
                Description = inscription.SessionFormation.Description,
                EstSelfPaced = inscription.SessionFormation.EstSelfPaced,
                EstPubliee = inscription.SessionFormation.EstPubliee,
                DateOuverture = inscription.SessionFormation.DateOuverture,
                DateFermeture = inscription.SessionFormation.DateFermeture,
                StatutAffichage = BuildSessionStatus(inscription.SessionFormation)
            }
            : detail.Sessions.FirstOrDefault(s => s.Id == detail.SessionId);

        var sessionAccess = BuildSessionAccessSnapshot(
            effectiveSession?.EstSelfPaced ?? false,
            effectiveSession?.DateOuverture,
            effectiveSession?.DateFermeture);

        var leconsTermineesIds = await db.ProgressionsLecon
            .AsNoTracking()
            .Where(p => p.ScoutId == scoutId && p.EstTerminee && p.Lecon.Module.FormationId == formationId)
            .Select(p => p.LeconId)
            .ToHashSetAsync();

        var quizzesReussisIds = await db.TentativesQuiz
            .AsNoTracking()
            .Where(t => t.ScoutId == scoutId && t.Reussi && t.Quiz.Module.FormationId == formationId)
            .Select(t => t.QuizId)
            .Distinct()
            .ToHashSetAsync();

        return new LearningAccessSnapshot(detail, leconsTermineesIds, quizzesReussisIds, sessionAccess);
    }

    private static SessionAccessSnapshot BuildSessionAccessSnapshot(
        bool estSelfPaced,
        DateTime? dateOuverture,
        DateTime? dateFermeture)
    {
        if (estSelfPaced)
            return new SessionAccessSnapshot(true, false, null);

        var now = DateTime.UtcNow;
        if (dateOuverture.HasValue && dateOuverture.Value > now)
        {
            return new SessionAccessSnapshot(
                false,
                true,
                $"Le parcours s'ouvrira le {dateOuverture.Value:dd/MM/yyyy}. Les contenus restent en lecture seule jusque-la.");
        }

        if (dateFermeture.HasValue && dateFermeture.Value < now)
        {
            return new SessionAccessSnapshot(
                false,
                true,
                $"La session est terminee depuis le {dateFermeture.Value:dd/MM/yyyy}. Le parcours est maintenant archive en lecture seule.");
        }

        return new SessionAccessSnapshot(true, false, null);
    }

    private static ModuleAvailabilitySnapshot BuildModuleAvailabilitySnapshot(
        ModuleDto module,
        IReadOnlyList<LeconDto> orderedLessons,
        HashSet<Guid> leconsTermineesIds,
        bool previousModulesCompleted,
        SessionAccessSnapshot sessionAccess,
        int nombreTentativesQuiz = 0)
    {
        var estDisponible = sessionAccess.PeutInteragir && previousModulesCompleted;
        var leconsDisponiblesIds = new HashSet<Guid>();
        var leconsSequentiallyUnlocked = estDisponible;

        foreach (var lecon in orderedLessons)
        {
            var estTerminee = leconsTermineesIds.Contains(lecon.Id);
            if (estTerminee || leconsSequentiallyUnlocked)
                leconsDisponiblesIds.Add(lecon.Id);

            leconsSequentiallyUnlocked = leconsSequentiallyUnlocked && estTerminee;
        }

        var toutesLeconsTerminees = orderedLessons.All(l => leconsTermineesIds.Contains(l.Id));
        var quizDisponible = estDisponible && toutesLeconsTerminees;

        string? messageBlocage = null;
        if (!sessionAccess.PeutInteragir)
            messageBlocage = sessionAccess.Message;
        else if (!previousModulesCompleted)
            messageBlocage = "Terminez le module precedent pour debloquer celui-ci.";

        string? messageQuiz = null;
        if (!sessionAccess.PeutInteragir)
            messageQuiz = sessionAccess.Message;
        else if (!previousModulesCompleted)
            messageQuiz = "Le quiz sera disponible apres validation du module precedent.";
        else if (!toutesLeconsTerminees)
            messageQuiz = "Terminez toutes les lecons du module avant de lancer le quiz.";
        else if (module.Quiz?.DateOuvertureDisponibilite.HasValue == true && module.Quiz.DateOuvertureDisponibilite.Value > DateTime.UtcNow)
        {
            quizDisponible = false;
            messageQuiz = $"Le quiz ouvrira le {module.Quiz.DateOuvertureDisponibilite.Value:dd/MM/yyyy HH:mm}.";
        }
        else if (module.Quiz?.DateFermetureDisponibilite.HasValue == true && module.Quiz.DateFermetureDisponibilite.Value < DateTime.UtcNow)
        {
            quizDisponible = false;
            messageQuiz = $"La fenetre d'evaluation s'est terminee le {module.Quiz.DateFermetureDisponibilite.Value:dd/MM/yyyy HH:mm}.";
        }
        else if (module.Quiz?.NombreTentativesMax.HasValue == true && nombreTentativesQuiz >= module.Quiz.NombreTentativesMax.Value)
        {
            quizDisponible = false;
            messageQuiz = $"Le nombre maximal de tentatives ({module.Quiz.NombreTentativesMax.Value}) a ete atteint.";
        }

        return new ModuleAvailabilitySnapshot(estDisponible, messageBlocage, leconsDisponiblesIds, quizDisponible, messageQuiz);
    }

    private static bool IsModuleComplete(int leconsTerminees, int totalLecons, bool aQuiz, bool quizReussi)
        => leconsTerminees >= totalLecons && (!aQuiz || quizReussi);

    private static string BuildSessionStatus(SessionFormation session)
    {
        if (session.EstSelfPaced)
            return "A votre rythme";

        var now = DateTime.UtcNow;
        if (session.DateOuverture.HasValue && session.DateOuverture.Value > now)
            return "Bientot";
        if (session.DateFermeture.HasValue && session.DateFermeture.Value < now)
            return "Terminee";
        if (session.DateOuverture.HasValue && session.DateOuverture.Value <= now)
            return "Session ouverte";

        return "Planifiee";
    }

    private static string BuildPedagogicalStatus(
        StatutInscription statut,
        int progressionPourcent,
        bool estSelfPaced,
        DateTime? dateOuverture,
        DateTime? dateFermeture)
    {
        var now = DateTime.UtcNow;
        if (statut == StatutInscription.Terminee || progressionPourcent >= 100)
            return "Parcours termine";
        if (!estSelfPaced && dateOuverture.HasValue && dateOuverture.Value > now)
            return "Session a venir";
        if (!estSelfPaced && dateFermeture.HasValue && dateFermeture.Value < now)
            return "Session cloturee";
        if (progressionPourcent == 0)
            return "Demarrage a planifier";
        if (progressionPourcent < 50)
            return "Fondamentaux en cours";
        if (progressionPourcent < 100)
            return "Finalisation du parcours";
        return "Parcours en cours";
    }

    private static string BuildEvaluationStatus(int quizTotal, int quizReussis, int? meilleurScoreQuiz)
    {
        if (quizTotal == 0)
            return "Sans quiz certificatif";
        if (quizReussis == quizTotal)
            return "Evaluation validee";
        if (meilleurScoreQuiz.HasValue)
            return $"{quizReussis}/{quizTotal} quiz valides";
        return "Quiz a demarrer";
    }

    private static string BuildCertificationStatus(
        bool delivreBadge,
        bool delivreAttestation,
        bool delivreCertificat,
        bool badgeObtenu,
        bool attestationObtenue,
        bool certificatObtenu,
        StatutInscription statut)
    {
        var parcoursCertifiant = delivreBadge || delivreAttestation || delivreCertificat;
        if (!parcoursCertifiant)
            return "Parcours non certifiant";
        if (certificatObtenu)
            return "Certificat emis";
        if (badgeObtenu || attestationObtenue)
            return "Delivrances disponibles";
        return statut == StatutInscription.Terminee
            ? "Delivrance activee"
            : "Parcours certifiant en cours";
    }

    private static string BuildNextStep(
        int progressionPourcent,
        int quizTotal,
        int quizReussis,
        StatutInscription statut,
        bool aDesCertifications,
        bool estSelfPaced,
        DateTime? dateOuverture)
    {
        var now = DateTime.UtcNow;
        if (!estSelfPaced && dateOuverture.HasValue && dateOuverture.Value > now)
            return $"Se preparer pour l'ouverture du {dateOuverture.Value:dd/MM/yyyy}";
        if (statut == StatutInscription.Terminee && aDesCertifications)
            return "Consulter vos badges et certificats";
        if (progressionPourcent == 0)
            return "Commencer le premier module";
        if (quizTotal > 0 && quizReussis < quizTotal && progressionPourcent >= 100)
            return "Valider les quiz restants";
        if (progressionPourcent < 100)
        {
            return quizTotal > 0 && quizReussis < quizTotal
                ? "Poursuivre les modules puis valider les quiz"
                : "Terminer les prochaines lecons";
        }

        return "Parcours termine";
    }

    private static DateTime? MaxDate(DateTime? first, DateTime? second)
    {
        if (!first.HasValue)
            return second;
        if (!second.HasValue)
            return first;
        return first >= second ? first : second;
    }

    private async Task EnsureCertificationsAsync(Formation formation, InscriptionFormation inscription, Guid scoutId)
    {
        var requestedTypes = new List<TypeCertificationFormation>();
        if (formation.DelivreBadge)
            requestedTypes.Add(TypeCertificationFormation.Badge);
        if (formation.DelivreAttestation)
            requestedTypes.Add(TypeCertificationFormation.Attestation);
        if (formation.DelivreCertificat)
            requestedTypes.Add(TypeCertificationFormation.Certificat);

        if (requestedTypes.Count == 0)
            return;

        var scoreFinal = await ComputeFinalScoreAsync(formation.Id, scoutId);
        var mention = BuildMention(scoreFinal);

        foreach (var type in requestedTypes)
        {
            var exists = await db.CertificationsFormation.AnyAsync(c =>
                c.ScoutId == scoutId &&
                c.FormationId == formation.Id &&
                c.Type == type);

            if (exists)
                continue;

            db.CertificationsFormation.Add(new CertificationFormation
            {
                Id = Guid.NewGuid(),
                Type = type,
                Code = BuildCertificateCode(type),
                DateEmission = DateTime.UtcNow,
                ScoreFinal = scoreFinal,
                Mention = mention,
                ScoutId = scoutId,
                FormationId = formation.Id,
                InscriptionFormationId = inscription.Id
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<int> ComputeFinalScoreAsync(Guid formationId, Guid scoutId)
    {
        var scores = await db.TentativesQuiz
            .AsNoTracking()
            .Where(t => t.ScoutId == scoutId && t.Quiz.Module.FormationId == formationId)
            .Select(t => t.Score)
            .ToListAsync();

        if (scores.Count == 0)
            return 100;

        return scores.Max();
    }

    private static string BuildMention(int scoreFinal)
    {
        if (scoreFinal >= 90)
            return "Excellent";
        if (scoreFinal >= 75)
            return "Tres bien";
        if (scoreFinal >= 60)
            return "Bien";
        return "Valide";
    }

    private static string BuildCertificateCode(TypeCertificationFormation type)
    {
        var prefix = type switch
        {
            TypeCertificationFormation.Badge => "BDG",
            TypeCertificationFormation.Attestation => "ATT",
            TypeCertificationFormation.Certificat => "CRT",
            _ => "LMS"
        };

        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private sealed record ActivitySnapshot(int Count, DateTime? LastActivity);
    private sealed record QuizProgressSnapshot(bool Reussi, int? MeilleurScore);
    private sealed record SessionAccessSnapshot(bool PeutInteragir, bool EstLectureSeule, string? Message);
    private sealed record ModuleAvailabilitySnapshot(
        bool EstDisponible,
        string? MessageBlocage,
        HashSet<Guid> LeconsDisponiblesIds,
        bool QuizDisponible,
        string? MessageQuiz);
    private sealed record LearningAccessSnapshot(
        FormationDetailDto Detail,
        HashSet<Guid> LeconsTermineesIds,
        HashSet<Guid> QuizzesReussisIds,
        SessionAccessSnapshot SessionAccess);
    private sealed record LessonAccessDecision(bool PeutInteragir, string? Message);
    private sealed record QuizAccessDecision(bool PeutInteragir, string? Message);

}
