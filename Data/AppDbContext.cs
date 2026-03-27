using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Scout> Scouts => Set<Scout>();
    public DbSet<Groupe> Groupes => Set<Groupe>();
    public DbSet<Branche> Branches => Set<Branche>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Competence> Competences => Set<Competence>();
    public DbSet<HistoriqueFonction> HistoriqueFonctions => Set<HistoriqueFonction>();
    public DbSet<Activite> Activites => Set<Activite>();
    public DbSet<DocumentActivite> DocumentsActivite => Set<DocumentActivite>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<MessageTicket> MessagesTicket => Set<MessageTicket>();
    public DbSet<TicketPieceJointe> TicketPiecesJointes => Set<TicketPieceJointe>();
    public DbSet<NotificationUtilisateur> NotificationsUtilisateur => Set<NotificationUtilisateur>();
    public DbSet<SupportServiceCatalogueItem> SupportCatalogueServices => Set<SupportServiceCatalogueItem>();
    public DbSet<SupportKnowledgeArticle> SupportKnowledgeArticles => Set<SupportKnowledgeArticle>();
    public DbSet<LivreDor> LivreDor => Set<LivreDor>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Galerie> Galeries => Set<Galerie>();
    public DbSet<MotCommissaire> MotsCommissaire => Set<MotCommissaire>();
    public DbSet<DemandeAutorisation> DemandesAutorisation => Set<DemandeAutorisation>();
    public DbSet<SuiviDemande> SuivisDemande => Set<SuiviDemande>();
    public DbSet<DemandeGroupe> DemandesGroupe => Set<DemandeGroupe>();
    public DbSet<MembreHistorique> MembresHistoriques => Set<MembreHistorique>();
    public DbSet<HistoriqueTicket> HistoriquesTicket => Set<HistoriqueTicket>();
    public DbSet<Actualite> Actualites => Set<Actualite>();
    public DbSet<CodeInvitation> CodesInvitation => Set<CodeInvitation>();
    public DbSet<ParticipantActivite> ParticipantsActivite => Set<ParticipantActivite>();
    public DbSet<CommentaireActivite> CommentairesActivite => Set<CommentaireActivite>();
    public DbSet<TransactionFinanciere> TransactionsFinancieres => Set<TransactionFinanciere>();
    public DbSet<ProjetAGR> ProjetsAGR => Set<ProjetAGR>();
    public DbSet<Partenaire> Partenaires => Set<Partenaire>();
    public DbSet<LienReseauSocial> LiensReseauxSociaux => Set<LienReseauSocial>();
    public DbSet<SuiviAcademique> SuivisAcademiques => Set<SuiviAcademique>();

    // LMS
    public DbSet<Formation> Formations => Set<Formation>();
    public DbSet<ModuleFormation> ModulesFormation => Set<ModuleFormation>();
    public DbSet<Lecon> Lecons => Set<Lecon>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuestionQuiz> QuestionsQuiz => Set<QuestionQuiz>();
    public DbSet<ReponseQuiz> ReponsesQuiz => Set<ReponseQuiz>();
    public DbSet<InscriptionFormation> InscriptionsFormation => Set<InscriptionFormation>();
    public DbSet<ProgressionLecon> ProgressionsLecon => Set<ProgressionLecon>();
    public DbSet<TentativeQuiz> TentativesQuiz => Set<TentativeQuiz>();
    public DbSet<SessionFormation> SessionsFormation => Set<SessionFormation>();
    public DbSet<AnnonceFormation> AnnoncesFormation => Set<AnnonceFormation>();
    public DbSet<CertificationFormation> CertificationsFormation => Set<CertificationFormation>();
    public DbSet<JalonFormation> JalonsFormation => Set<JalonFormation>();
    public DbSet<FormationPrerequis> FormationsPrerequis => Set<FormationPrerequis>();
    public DbSet<DiscussionFormation> DiscussionsFormation => Set<DiscussionFormation>();
    public DbSet<MessageDiscussionFormation> MessagesDiscussionFormation => Set<MessageDiscussionFormation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("citext");
        builder.HasPostgresExtension("unaccent");
        builder.HasDbFunction(typeof(PostgresTextFunctions).GetMethod(nameof(PostgresTextFunctions.NormalizeSearch), [typeof(string)])!)
            .HasName("normalize_text_search");

        // Scout - matricule unique
        builder.Entity<Scout>(e =>
        {
            e.Property(s => s.Matricule).HasColumnType("citext");
            e.Property(s => s.NumeroCarte).HasColumnType("citext");
            e.HasIndex(s => s.Matricule)
                .HasDatabaseName(PersistenceConstraints.ScoutsMatricule)
                .IsUnique();
            e.HasIndex(s => s.NumeroCarte)
                .HasDatabaseName(PersistenceConstraints.ScoutsNumeroCarte)
                .IsUnique()
                .HasFilter("\"NumeroCarte\" IS NOT NULL");
            e.HasOne(s => s.Groupe).WithMany().HasForeignKey(s => s.GroupeId);
            e.HasOne(s => s.Branche).WithMany(b => b.Scouts).HasForeignKey(s => s.BrancheId);
            e.HasMany(s => s.Parents).WithMany(p => p.Scouts);
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // Groupe
        builder.Entity<Groupe>(e =>
        {
            e.Property(g => g.NomNormalise)
                .HasMaxLength(256)
                .HasComputedColumnSql("left(normalize_text_search(\"Nom\"), 256)", stored: true);
            e.HasIndex(g => g.NomNormalise)
                .HasDatabaseName(PersistenceConstraints.GroupesNomNormaliseActif)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
            e.HasOne(g => g.Responsable).WithMany().HasForeignKey(g => g.ResponsableId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(g => g.Membres).WithOne(u => u.Groupe).HasForeignKey(u => u.GroupeId);
            e.HasMany(g => g.Branches).WithOne(b => b.Groupe).HasForeignKey(b => b.GroupeId);
        });

        // Branche
        builder.Entity<Branche>(e =>
        {
            e.Property(b => b.NomNormalise)
                .HasMaxLength(256)
                .HasComputedColumnSql("left(normalize_text_search(\"Nom\"), 256)", stored: true);
            e.HasIndex(b => new { b.GroupeId, b.NomNormalise })
                .HasDatabaseName(PersistenceConstraints.BranchesGroupeNomNormaliseActif)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
            e.HasOne(b => b.ChefUnite).WithMany().HasForeignKey(b => b.ChefUniteId).OnDelete(DeleteBehavior.SetNull);
        });

        // Activite
        builder.Entity<Activite>(e =>
        {
            e.HasOne(a => a.Createur).WithMany().HasForeignKey(a => a.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Groupe).WithMany().HasForeignKey(a => a.GroupeId);
            e.HasMany(a => a.Documents).WithOne(d => d.Activite).HasForeignKey(d => d.ActiviteId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(a => a.Participants).WithOne(p => p.Activite).HasForeignKey(p => p.ActiviteId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(a => a.Commentaires).WithOne(c => c.Activite).HasForeignKey(c => c.ActiviteId).OnDelete(DeleteBehavior.Cascade);
        });

        // ParticipantActivite
        builder.Entity<ParticipantActivite>(e =>
        {
            e.HasOne(p => p.Scout).WithMany().HasForeignKey(p => p.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.ActiviteId, p.ScoutId }).IsUnique();
        });

        // CommentaireActivite
        builder.Entity<CommentaireActivite>(e =>
        {
            e.HasOne(c => c.Auteur).WithMany().HasForeignKey(c => c.AuteurId).OnDelete(DeleteBehavior.Restrict);
        });

        // Ticket
        builder.Entity<Ticket>(e =>
        {
            e.HasOne(t => t.Createur).WithMany(u => u.Tickets).HasForeignKey(t => t.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.AssigneA).WithMany().HasForeignKey(t => t.AssigneAId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.GroupeAssigne).WithMany().HasForeignKey(t => t.GroupeAssigneId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.ServiceCatalogue).WithMany(s => s.Tickets).HasForeignKey(t => t.ServiceCatalogueId).OnDelete(DeleteBehavior.SetNull);
        });

        // MessageTicket
        builder.Entity<MessageTicket>(e =>
        {
            e.HasOne(m => m.Auteur).WithMany().HasForeignKey(m => m.AuteurId).OnDelete(DeleteBehavior.Restrict);
        });

        // TicketPieceJointe
        builder.Entity<TicketPieceJointe>(e =>
        {
            e.HasOne(p => p.Ticket).WithMany(t => t.PiecesJointes).HasForeignKey(p => p.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.AjoutePar).WithMany().HasForeignKey(p => p.AjouteParId).OnDelete(DeleteBehavior.Restrict);
        });

        // NotificationUtilisateur
        builder.Entity<NotificationUtilisateur>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.EstLue, n.DateCreation });
            e.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // SupportServiceCatalogueItem
        builder.Entity<SupportServiceCatalogueItem>(e =>
        {
            e.Property(s => s.Code).HasColumnType("citext");
            e.HasIndex(s => s.Code).IsUnique();
            e.HasOne(s => s.Auteur).WithMany().HasForeignKey(s => s.AuteurId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.AssigneParDefaut).WithMany().HasForeignKey(s => s.AssigneParDefautId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.GroupeParDefaut).WithMany().HasForeignKey(s => s.GroupeParDefautId).OnDelete(DeleteBehavior.SetNull);
        });

        // SupportKnowledgeArticle
        builder.Entity<SupportKnowledgeArticle>(e =>
        {
            e.HasOne(a => a.Auteur).WithMany().HasForeignKey(a => a.AuteurId).OnDelete(DeleteBehavior.SetNull);
        });

        // DemandeAutorisation
        builder.Entity<DemandeAutorisation>(e =>
        {
            e.HasOne(d => d.Demandeur).WithMany().HasForeignKey(d => d.DemandeurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Valideur).WithMany().HasForeignKey(d => d.ValideurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Groupe).WithMany().HasForeignKey(d => d.GroupeId);
            e.HasMany(d => d.Suivis).WithOne(s => s.Demande).HasForeignKey(s => s.DemandeId).OnDelete(DeleteBehavior.Cascade);
        });

        // DemandeGroupe
        builder.Entity<DemandeGroupe>(e =>
        {
            e.HasOne(d => d.TraitePar).WithMany().HasForeignKey(d => d.TraiteParId).OnDelete(DeleteBehavior.Restrict);
        });

        // HistoriqueTicket
        builder.Entity<HistoriqueTicket>(e =>
        {
            e.HasOne(h => h.Ticket).WithMany(t => t.Historiques).HasForeignKey(h => h.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.Auteur).WithMany().HasForeignKey(h => h.AuteurId).OnDelete(DeleteBehavior.Restrict);
        });

        // Actualite
        builder.Entity<Actualite>(e =>
        {
            e.HasOne(a => a.Createur).WithMany().HasForeignKey(a => a.CreateurId).OnDelete(DeleteBehavior.Restrict);
        });

        // CodeInvitation
        builder.Entity<CodeInvitation>(e =>
        {
            e.Property(c => c.Code).HasColumnType("citext");
            e.HasIndex(c => c.Code).IsUnique();
            e.HasOne(c => c.Createur).WithMany().HasForeignKey(c => c.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.UtilisePar).WithMany().HasForeignKey(c => c.UtilisePaId).OnDelete(DeleteBehavior.Restrict);
        });

        // TransactionFinanciere
        builder.Entity<TransactionFinanciere>(e =>
        {
            e.HasOne(t => t.Createur).WithMany().HasForeignKey(t => t.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Groupe).WithMany().HasForeignKey(t => t.GroupeId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Activite).WithMany().HasForeignKey(t => t.ActiviteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.ProjetAGR).WithMany(p => p.Transactions).HasForeignKey(t => t.ProjetAGRId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Scout).WithMany(s => s.Cotisations).HasForeignKey(t => t.ScoutId).OnDelete(DeleteBehavior.SetNull);
        });

        // ProjetAGR
        builder.Entity<ProjetAGR>(e =>
        {
            e.HasOne(p => p.Createur).WithMany().HasForeignKey(p => p.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Groupe).WithMany().HasForeignKey(p => p.GroupeId).OnDelete(DeleteBehavior.SetNull);
        });

        // SuiviAcademique
        builder.Entity<SuiviAcademique>(e =>
        {
            e.HasOne(s => s.Scout).WithMany(sc => sc.SuivisAcademiques).HasForeignKey(s => s.ScoutId).OnDelete(DeleteBehavior.Cascade);
        });

        // === LMS ===

        // Formation
        builder.Entity<Formation>(e =>
        {
            e.HasOne(f => f.Auteur).WithMany().HasForeignKey(f => f.AuteurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.BrancheCible).WithMany().HasForeignKey(f => f.BrancheCibleId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(f => f.Modules).WithOne(m => m.Formation).HasForeignKey(m => m.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Inscriptions).WithOne(i => i.Formation).HasForeignKey(i => i.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Sessions).WithOne(s => s.Formation).HasForeignKey(s => s.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Annonces).WithOne(a => a.Formation).HasForeignKey(a => a.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Certifications).WithOne(c => c.Formation).HasForeignKey(c => c.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Jalons).WithOne(j => j.Formation).HasForeignKey(j => j.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Discussions).WithOne(d => d.Formation).HasForeignKey(d => d.FormationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FormationPrerequis>(e =>
        {
            e.HasKey(p => new { p.FormationId, p.PrerequisFormationId });
            e.HasOne(p => p.Formation)
                .WithMany(f => f.Prerequis)
                .HasForeignKey(p => p.FormationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.PrerequisFormation)
                .WithMany(f => f.FormationDebloqueesParCeCours)
                .HasForeignKey(p => p.PrerequisFormationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SessionFormation>(e =>
        {
            e.HasIndex(s => new { s.FormationId, s.EstPubliee, s.DateOuverture });
        });

        builder.Entity<AnnonceFormation>(e =>
        {
            e.HasIndex(a => new { a.FormationId, a.EstPubliee, a.DatePublication });
            e.HasOne(a => a.Auteur).WithMany().HasForeignKey(a => a.AuteurId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<JalonFormation>(e =>
        {
            e.HasIndex(j => new { j.FormationId, j.DateJalon });
        });

        // ModuleFormation
        builder.Entity<ModuleFormation>(e =>
        {
            e.HasMany(m => m.Lecons).WithOne(l => l.Module).HasForeignKey(l => l.ModuleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Quiz).WithOne(q => q.Module).HasForeignKey<Quiz>(q => q.ModuleId).OnDelete(DeleteBehavior.Cascade);
        });

        // Lecon
        builder.Entity<Lecon>(e =>
        {
            e.HasMany(l => l.Progressions).WithOne(p => p.Lecon).HasForeignKey(p => p.LeconId).OnDelete(DeleteBehavior.Cascade);
        });

        // Quiz
        builder.Entity<Quiz>(e =>
        {
            e.HasMany(q => q.Questions).WithOne(qu => qu.Quiz).HasForeignKey(qu => qu.QuizId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(q => q.Tentatives).WithOne(t => t.Quiz).HasForeignKey(t => t.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        // QuestionQuiz
        builder.Entity<QuestionQuiz>(e =>
        {
            e.HasMany(q => q.Reponses).WithOne(r => r.Question).HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        // InscriptionFormation — unique par scout/formation
        builder.Entity<InscriptionFormation>(e =>
        {
            e.HasOne(i => i.Scout).WithMany().HasForeignKey(i => i.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.SessionFormation).WithMany(s => s.Inscriptions).HasForeignKey(i => i.SessionFormationId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(i => new { i.ScoutId, i.FormationId }).IsUnique();
        });

        builder.Entity<CertificationFormation>(e =>
        {
            e.Property(c => c.Code).HasColumnType("citext");
            e.HasIndex(c => c.Code).IsUnique();
            e.HasIndex(c => new { c.ScoutId, c.FormationId, c.Type }).IsUnique();
            e.HasOne(c => c.Scout).WithMany().HasForeignKey(c => c.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.InscriptionFormation).WithMany(i => i.Certifications).HasForeignKey(c => c.InscriptionFormationId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DiscussionFormation>(e =>
        {
            e.HasIndex(d => new { d.FormationId, d.DateDerniereActivite });
            e.HasOne(d => d.Auteur).WithMany().HasForeignKey(d => d.AuteurId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(d => d.Messages).WithOne(m => m.Discussion).HasForeignKey(m => m.DiscussionFormationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MessageDiscussionFormation>(e =>
        {
            e.HasIndex(m => new { m.DiscussionFormationId, m.DateCreation });
            e.HasOne(m => m.Auteur).WithMany().HasForeignKey(m => m.AuteurId).OnDelete(DeleteBehavior.Restrict);
        });

        // ProgressionLecon — unique par scout/leçon
        builder.Entity<ProgressionLecon>(e =>
        {
            e.HasOne(p => p.Scout).WithMany().HasForeignKey(p => p.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.ScoutId, p.LeconId }).IsUnique();
        });

        // TentativeQuiz
        builder.Entity<TentativeQuiz>(e =>
        {
            e.HasOne(t => t.Scout).WithMany().HasForeignKey(t => t.ScoutId).OnDelete(DeleteBehavior.Cascade);
        });

        // Seed des rôles
        var roleData = new (string Name, string Guid)[]
        {
            ("Administrateur", "a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            ("Gestionnaire",   "b2c3d4e5-f6a7-8901-bcde-f12345678901"),
            ("AgentSupport",   "91b7eb6f-c8de-4dd3-9785-f0e6f7932301"),
            ("Scout",          "c3d4e5f6-a7b8-9012-cdef-123456789012"),
            ("Parent",         "d4e5f6a7-b8c9-0123-defa-234567890123"),
            ("Superviseur",    "f6a7b8c9-d0e1-2345-fabc-456789012345"),
            ("Consultant",     "e5f6a7b8-c9d0-1234-efab-345678901234")
        };
        foreach (var (name, guid) in roleData)
        {
            builder.Entity<IdentityRole<Guid>>().HasData(new IdentityRole<Guid>
            {
                Id = Guid.Parse(guid),
                Name = name,
                NormalizedName = name.ToUpperInvariant()
            });
        }
    }
}
