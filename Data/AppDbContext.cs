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
    public DbSet<MembreHistoriqueCategorie> MembresHistoriquesCategories => Set<MembreHistoriqueCategorie>();
    public DbSet<HistoriqueTicket> HistoriquesTicket => Set<HistoriqueTicket>();
    public DbSet<Actualite> Actualites => Set<Actualite>();
    public DbSet<CodeInvitation> CodesInvitation => Set<CodeInvitation>();
    public DbSet<ParticipantActivite> ParticipantsActivite => Set<ParticipantActivite>();
    public DbSet<Ressource> Ressources => Set<Ressource>();
    public DbSet<ParticipationFormationRessource> ParticipationsFormationRessources => Set<ParticipationFormationRessource>();
    public DbSet<CommentaireActivite> CommentairesActivite => Set<CommentaireActivite>();
    public DbSet<TransactionFinanciere> TransactionsFinancieres => Set<TransactionFinanciere>();
    public DbSet<ProjetAGR> ProjetsAGR => Set<ProjetAGR>();
    public DbSet<Partenaire> Partenaires => Set<Partenaire>();
    public DbSet<LienReseauSocial> LiensReseauxSociaux => Set<LienReseauSocial>();
    public DbSet<SuiviAcademique> SuivisAcademiques => Set<SuiviAcademique>();
    public DbSet<EtapeParcoursScout> EtapesParcoursScouts => Set<EtapeParcoursScout>();
    public DbSet<ModeleEtapeParcours> ModelesEtapesParcours => Set<ModeleEtapeParcours>();
    public DbSet<InscriptionAnnuelleScout> InscriptionsAnnuellesScouts => Set<InscriptionAnnuelleScout>();
    public DbSet<ProgrammeAnnuel> ProgrammesAnnuels => Set<ProgrammeAnnuel>();
    public DbSet<ProgrammeAnnuelActivite> ProgrammesAnnuelsActivites => Set<ProgrammeAnnuelActivite>();
    public DbSet<RapportActivite> RapportsActivite => Set<RapportActivite>();
    public DbSet<RapportActivitePieceJointe> RapportsActivitePiecesJointes => Set<RapportActivitePieceJointe>();
    public DbSet<PropositionMaitriseAnnuelle> PropositionsMaitriseAnnuelles => Set<PropositionMaitriseAnnuelle>();
    public DbSet<PropositionMaitriseMembre> PropositionsMaitriseMembres => Set<PropositionMaitriseMembre>();
    public DbSet<CotisationNationaleImport> CotisationsNationalesImports => Set<CotisationNationaleImport>();
    public DbSet<CotisationNationaleImportLigne> CotisationsNationalesImportLignes => Set<CotisationNationaleImportLigne>();
    public DbSet<PortefeuilleUtilisateur> PortefeuillesUtilisateurs => Set<PortefeuilleUtilisateur>();
    public DbSet<MouvementPortefeuille> MouvementsPortefeuilles => Set<MouvementPortefeuille>();
    public DbSet<ComptePaiementMobile> ComptesPaiementMobile => Set<ComptePaiementMobile>();
    public DbSet<ProfilAbonnement> ProfilsAbonnements => Set<ProfilAbonnement>();
    public DbSet<AbonnementUtilisateur> AbonnementsUtilisateurs => Set<AbonnementUtilisateur>();
    public DbSet<DonPublic> DonsPublics => Set<DonPublic>();
    public DbSet<ArticleBoutique> ArticlesBoutique => Set<ArticleBoutique>();
    public DbSet<CommandeBoutique> CommandesBoutique => Set<CommandeBoutique>();
    public DbSet<LigneCommandeBoutique> LignesCommandesBoutique => Set<LigneCommandeBoutique>();
    public DbSet<RegionScoute> RegionsScoutes => Set<RegionScoute>();
    public DbSet<DistrictScout> DistrictsScouts => Set<DistrictScout>();
    public DbSet<UniteScoute> UnitesScoutes => Set<UniteScoute>();
    public DbSet<RoleUniteScoute> RolesUnitesScoutes => Set<RoleUniteScoute>();
    public DbSet<AffectationUniteScoute> AffectationsUnitesScoutes => Set<AffectationUniteScoute>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();

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
                .IsUnique()
                .HasFilter("\"Matricule\" IS NOT NULL");
            e.HasIndex(s => s.NumeroCarte)
                .HasDatabaseName(PersistenceConstraints.ScoutsNumeroCarte)
                .IsUnique()
                .HasFilter("\"NumeroCarte\" IS NOT NULL");
            e.HasOne(s => s.Groupe).WithMany().HasForeignKey(s => s.GroupeId);
            e.HasOne(s => s.Branche).WithMany(b => b.Scouts).HasForeignKey(s => s.BrancheId);
            e.HasMany(s => s.Parents).WithMany(p => p.Scouts);
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Permission>(e =>
        {
            e.Property(p => p.Code).HasMaxLength(160);
            e.Property(p => p.Libelle).HasMaxLength(180);
            e.Property(p => p.Module).HasMaxLength(80);
            e.HasIndex(p => p.Code).IsUnique();
        });

        builder.Entity<RolePermission>(e =>
        {
            e.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
            e.HasOne(rp => rp.Role).WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SecurityAuditLog>(e =>
        {
            e.Property(a => a.Action).HasMaxLength(120);
            e.Property(a => a.AncienneValeur).HasMaxLength(1200);
            e.Property(a => a.NouvelleValeur).HasMaxLength(1200);
            e.Property(a => a.Commentaire).HasMaxLength(1200);
            e.Property(a => a.AdresseIp).HasMaxLength(80);
            e.HasIndex(a => new { a.UtilisateurCibleId, a.DateCreation });
            e.HasOne(a => a.Auteur).WithMany().HasForeignKey(a => a.AuteurId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.UtilisateurCible).WithMany().HasForeignKey(a => a.UtilisateurCibleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Parent>(e =>
        {
            e.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Parents_UserId");
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
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
            e.HasOne(g => g.DistrictScout).WithMany(d => d.Groupes).HasForeignKey(g => g.DistrictScoutId).OnDelete(DeleteBehavior.SetNull);
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

        builder.Entity<UniteScoute>(e =>
        {
            e.HasIndex(u => new { u.BrancheId, u.Nom, u.EstSupprime });
            e.HasOne(u => u.Groupe).WithMany().HasForeignKey(u => u.GroupeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.Branche).WithMany().HasForeignKey(u => u.BrancheId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.Createur).WithMany().HasForeignKey(u => u.CreateurId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(u => u.Roles).WithOne(r => r.UniteScoute).HasForeignKey(r => r.UniteScouteId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.Affectations).WithOne(a => a.UniteScoute).HasForeignKey(a => a.UniteScouteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RoleUniteScoute>(e =>
        {
            e.HasIndex(r => new { r.UniteScouteId, r.Nom }).IsUnique().HasFilter("\"EstSupprime\" = FALSE");
        });

        builder.Entity<AffectationUniteScoute>(e =>
        {
            e.HasOne(a => a.Scout).WithMany(s => s.AffectationsUnites).HasForeignKey(a => a.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.RoleUniteScoute).WithMany().HasForeignKey(a => a.RoleUniteScouteId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => a.ScoutId).IsUnique().HasFilter("\"EstActif\" = TRUE");
            e.HasIndex(a => new { a.UniteScouteId, a.ScoutId }).IsUnique().HasFilter("\"EstActif\" = TRUE");
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
            e.HasOne(p => p.Ressource).WithMany(r => r.ParticipationsActivites).HasForeignKey(p => p.RessourceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.ActiviteId, p.ScoutId }).IsUnique().HasFilter("\"ScoutId\" IS NOT NULL AND \"EstSupprime\" = FALSE");
            e.HasIndex(p => new { p.ActiviteId, p.RessourceId }).IsUnique().HasFilter("\"RessourceId\" IS NOT NULL AND \"EstSupprime\" = FALSE");
            e.ToTable(t => t.HasCheckConstraint("CK_ParticipantsActivite_ParticipantType", "(\"ScoutId\" IS NOT NULL AND \"RessourceId\" IS NULL) OR (\"ScoutId\" IS NULL AND \"RessourceId\" IS NOT NULL)"));
        });

        builder.Entity<Ressource>(e =>
        {
            e.HasOne(r => r.Groupe).WithMany().HasForeignKey(r => r.GroupeId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(r => new { r.GroupeId, r.Type, r.Nom, r.Prenom });
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
            e.HasOne(d => d.Branche).WithMany().HasForeignKey(d => d.BrancheId).OnDelete(DeleteBehavior.SetNull);
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

        // MembreHistorique
        builder.Entity<MembreHistorique>(e =>
        {
            e.Property(m => m.Categories).HasColumnName("Categorie");
            e.HasMany(m => m.CategorieDetails)
                .WithOne(d => d.MembreHistorique)
                .HasForeignKey(d => d.MembreHistoriqueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MembreHistoriqueCategorie>(e =>
        {
            e.HasIndex(d => new { d.MembreHistoriqueId, d.Categorie })
                .IsUnique();
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

        // Parcours scout
        builder.Entity<EtapeParcoursScout>(e =>
        {
            e.HasOne(p => p.Scout).WithMany(s => s.EtapesParcours).HasForeignKey(p => p.ScoutId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ModeleEtapeParcours>(e =>
        {
            e.HasOne(m => m.Branche).WithMany().HasForeignKey(m => m.BrancheId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(m => new { m.BrancheId, m.CodeEtape, m.IsActive })
                .HasDatabaseName("IX_ModeleEtapeParcours_Branche_Code_Actif")
                .HasFilter("\"CodeEtape\" IS NOT NULL");
            e.HasIndex(m => new { m.BrancheId, m.OrdreAffichage, m.IsActive })
                .HasDatabaseName("IX_ModeleEtapeParcours_Branche_Ordre_Actif");
        });

        // Inscriptions annuelles
        builder.Entity<InscriptionAnnuelleScout>(e =>
        {
            e.HasIndex(i => new { i.ScoutId, i.AnneeReference }).IsUnique();
            e.HasOne(i => i.Scout).WithMany().HasForeignKey(i => i.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.ValidePar).WithMany().HasForeignKey(i => i.ValideParId).OnDelete(DeleteBehavior.SetNull);
        });

        // Programme annuel
        builder.Entity<ProgrammeAnnuel>(e =>
        {
            e.HasIndex(p => new { p.GroupeId, p.AnneeReference }).IsUnique();
            e.HasOne(p => p.Groupe).WithMany().HasForeignKey(p => p.GroupeId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Createur).WithMany().HasForeignKey(p => p.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Valideur).WithMany().HasForeignKey(p => p.ValideurId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(p => p.Activites).WithOne(a => a.ProgrammeAnnuel).HasForeignKey(a => a.ProgrammeAnnuelId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProgrammeAnnuelActivite>(e =>
        {
            e.HasIndex(a => new { a.ProgrammeAnnuelId, a.OrdreAffichage });
            e.HasOne(a => a.Branche).WithMany().HasForeignKey(a => a.BrancheId).OnDelete(DeleteBehavior.SetNull);
        });

        // Rapport d'activite
        builder.Entity<RapportActivite>(e =>
        {
            e.HasIndex(r => r.ActiviteId).IsUnique();
            e.HasOne(r => r.Activite).WithMany().HasForeignKey(r => r.ActiviteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Createur).WithMany().HasForeignKey(r => r.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Valideur).WithMany().HasForeignKey(r => r.ValideurId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(r => r.PiecesJointes).WithOne(p => p.RapportActivite).HasForeignKey(p => p.RapportActiviteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RapportActivitePieceJointe>(e =>
        {
            e.HasIndex(p => new { p.RapportActiviteId, p.DateAjout });
        });

        // Proposition annuelle de maitrise
        builder.Entity<PropositionMaitriseAnnuelle>(e =>
        {
            e.HasIndex(p => new { p.GroupeId, p.AnneeReference }).IsUnique();
            e.HasOne(p => p.Groupe).WithMany().HasForeignKey(p => p.GroupeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Createur).WithMany().HasForeignKey(p => p.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Valideur).WithMany().HasForeignKey(p => p.ValideurId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(p => p.Membres).WithOne(m => m.PropositionMaitriseAnnuelle).HasForeignKey(m => m.PropositionMaitriseAnnuelleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PropositionMaitriseMembre>(e =>
        {
            e.HasIndex(m => new { m.PropositionMaitriseAnnuelleId, m.OrdreAffichage });
            e.HasOne(m => m.Branche).WithMany().HasForeignKey(m => m.BrancheId).OnDelete(DeleteBehavior.SetNull);
        });

        // Imports de cotisations nationales
        builder.Entity<CotisationNationaleImport>(e =>
        {
            e.HasIndex(i => new { i.AnneeReference, i.DateImport });
            e.HasOne(i => i.Createur).WithMany().HasForeignKey(i => i.CreateurId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(i => i.Lignes).WithOne(l => l.Import).HasForeignKey(l => l.ImportId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CotisationNationaleImportLigne>(e =>
        {
            e.HasIndex(l => new { l.ImportId, l.Matricule });
            e.HasOne(l => l.Scout).WithMany().HasForeignKey(l => l.ScoutId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(u => u.DistrictScout).WithMany().HasForeignKey(u => u.DistrictScoutId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(u => u.Portefeuille).WithOne(p => p.User).HasForeignKey<PortefeuilleUtilisateur>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PortefeuilleUtilisateur>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.Property(p => p.Solde).HasPrecision(18, 2);
        });

        builder.Entity<MouvementPortefeuille>(e =>
        {
            e.Property(m => m.Montant).HasPrecision(18, 2);
            e.Property(m => m.SoldeAvant).HasPrecision(18, 2);
            e.Property(m => m.SoldeApres).HasPrecision(18, 2);
            e.HasIndex(m => new { m.PortefeuilleUtilisateurId, m.DateCreation });
            e.HasIndex(m => m.TransfertId);
            e.HasIndex(m => m.RecuToken);
            e.HasOne(m => m.PortefeuilleUtilisateur).WithMany(p => p.Mouvements).HasForeignKey(m => m.PortefeuilleUtilisateurId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.ValidePar).WithMany().HasForeignKey(m => m.ValideParId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(m => m.TransactionFinanciere).WithMany().HasForeignKey(m => m.TransactionFinanciereId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ComptePaiementMobile>(e =>
        {
            e.HasIndex(c => new { c.NumeroMobile, c.EstActif, c.EstSupprime });
            e.HasOne(c => c.ModifiePar).WithMany().HasForeignKey(c => c.ModifieParId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Activite).WithMany().HasForeignKey(c => c.ActiviteId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProfilAbonnement>(e =>
        {
            e.Property(p => p.Montant).HasPrecision(18, 2);
            e.HasIndex(p => new { p.NomProfil, p.EstSupprime });
            e.HasOne(p => p.ComptePaiementMobile).WithMany().HasForeignKey(p => p.ComptePaiementMobileId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AbonnementUtilisateur>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.ProfilAbonnementId, a.EstSupprime });
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.ProfilAbonnement).WithMany(p => p.Abonnements).HasForeignKey(a => a.ProfilAbonnementId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DonPublic>(e =>
        {
            e.Property(d => d.Montant).HasPrecision(18, 2);
            e.HasIndex(d => new { d.Statut, d.DateCreation });
            e.HasIndex(d => d.RecuToken);
            e.HasOne(d => d.TraitePar).WithMany().HasForeignKey(d => d.TraiteParId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.TransactionFinanciere).WithMany().HasForeignKey(d => d.TransactionFinanciereId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ArticleBoutique>(e =>
        {
            e.Property(a => a.Prix).HasPrecision(18, 2);
            e.HasIndex(a => new { a.EstPublie, a.EstSupprime });
            e.HasIndex(a => a.Categorie);
        });

        builder.Entity<CommandeBoutique>(e =>
        {
            e.Property(c => c.Total).HasPrecision(18, 2);
            e.HasIndex(c => new { c.Statut, c.DateCreation });
            e.HasIndex(c => c.RecuToken);
            e.HasOne(c => c.Client).WithMany().HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.TraitePar).WithMany().HasForeignKey(c => c.TraiteParId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(c => c.Lignes).WithOne(l => l.CommandeBoutique).HasForeignKey(l => l.CommandeBoutiqueId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LigneCommandeBoutique>(e =>
        {
            e.Property(l => l.PrixUnitaire).HasPrecision(18, 2);
            e.HasOne(l => l.ArticleBoutique).WithMany().HasForeignKey(l => l.ArticleBoutiqueId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RegionScoute>(e =>
        {
            e.HasIndex(r => r.Nom);
        });

        builder.Entity<DistrictScout>(e =>
        {
            e.HasIndex(d => d.Nom);
            e.HasOne(d => d.RegionScoute).WithMany(r => r.Districts).HasForeignKey(d => d.RegionScouteId).OnDelete(DeleteBehavior.SetNull);
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

        // InscriptionFormation : unique par scout/formation
        builder.Entity<InscriptionFormation>(e =>
        {
            e.HasOne(i => i.Scout).WithMany().HasForeignKey(i => i.ScoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.SessionFormation).WithMany(s => s.Inscriptions).HasForeignKey(i => i.SessionFormationId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(i => new { i.ScoutId, i.FormationId }).IsUnique();
        });

        builder.Entity<ParticipationFormationRessource>(e =>
        {
            e.HasOne(p => p.Ressource).WithMany(r => r.ParticipationsFormation).HasForeignKey(p => p.RessourceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Formation).WithMany().HasForeignKey(p => p.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.RessourceId, p.FormationId }).IsUnique().HasFilter("\"EstSupprime\" = FALSE");
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

        // ProgressionLecon : unique par scout/leçon
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

        builder.Entity<Actualite>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<AbonnementUtilisateur>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Activite>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ArticleBoutique>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Competence>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ContactMessage>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<DocumentActivite>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<EtapeParcoursScout>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Formation>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Galerie>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<LienReseauSocial>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<LivreDor>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<MembreHistorique>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<MembreHistoriqueCategorie>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<MessageDiscussionFormation>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ModuleFormation>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<MotCommissaire>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ParticipantActivite>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Partenaire>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ProfilAbonnement>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ProgrammeAnnuel>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ProgrammeAnnuelActivite>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<ProjetAGR>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<QuestionQuiz>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Quiz>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<RapportActivitePieceJointe>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<SessionFormation>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<SuiviAcademique>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Ticket>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<TransactionFinanciere>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<UniteScoute>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<RoleUniteScoute>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<AffectationUniteScoute>().HasQueryFilter(e => e.EstActif);
        builder.Entity<AnnonceFormation>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<JalonFormation>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<Lecon>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<PropositionMaitriseAnnuelle>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<PropositionMaitriseMembre>().HasQueryFilter(e => !e.EstSupprime);
        builder.Entity<CertificationFormation>().HasQueryFilter(e => !e.Formation.EstSupprime);
        builder.Entity<CommentaireActivite>().HasQueryFilter(e => !e.Activite.EstSupprime);
        builder.Entity<DiscussionFormation>().HasQueryFilter(e => !e.Formation.EstSupprime);
        builder.Entity<FormationPrerequis>().HasQueryFilter(e => !e.Formation.EstSupprime && !e.PrerequisFormation.EstSupprime);
        builder.Entity<HistoriqueTicket>().HasQueryFilter(e => !e.Ticket.EstSupprime);
        builder.Entity<InscriptionFormation>().HasQueryFilter(e => !e.Formation.EstSupprime);
        builder.Entity<LigneCommandeBoutique>().HasQueryFilter(e => !e.ArticleBoutique.EstSupprime);
        builder.Entity<MessageTicket>().HasQueryFilter(e => !e.Ticket.EstSupprime);
        builder.Entity<ParticipationFormationRessource>().HasQueryFilter(e => !e.EstSupprime && !e.Formation.EstSupprime);
        builder.Entity<ProgressionLecon>().HasQueryFilter(e => !e.Lecon.EstSupprime);
        builder.Entity<RapportActivite>().HasQueryFilter(e => !e.Activite.EstSupprime);
        builder.Entity<ReponseQuiz>().HasQueryFilter(e => !e.Question.EstSupprime);
        builder.Entity<TentativeQuiz>().HasQueryFilter(e => !e.Quiz.EstSupprime);
        builder.Entity<TicketPieceJointe>().HasQueryFilter(e => !e.Ticket.EstSupprime);

        // Seed des rôles
        var roleData = new (string Name, string Guid)[]
        {
            ("Administrateur", "a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            ("Gestionnaire",   "b2c3d4e5-f6a7-8901-bcde-f12345678901"),
            ("AgentSupport",   "91b7eb6f-c8de-4dd3-9785-f0e6f7932301"),
            ("Scout",          "c3d4e5f6-a7b8-9012-cdef-123456789012"),
            ("Parent",         "d4e5f6a7-b8c9-0123-defa-234567890123"),
            ("Superviseur",            "f6a7b8c9-d0e1-2345-fabc-456789012345"),
            ("Consultant",             "e5f6a7b8-c9d0-1234-efab-345678901234"),
            ("CommissaireDistrict",    "aa000004-bbbb-cccc-dddd-000000000004"),
            ("CommissaireDistrictAdjoint", "aa000005-bbbb-cccc-dddd-000000000005"),
            ("AssistantCommissaireDistrict", "aa000006-bbbb-cccc-dddd-000000000006"),
            ("EquipeDistrict",         "aa000001-bbbb-cccc-dddd-000000000001"),
            ("ChefGroupe",             "aa000002-bbbb-cccc-dddd-000000000002"),
            ("ChefUnite",              "aa000003-bbbb-cccc-dddd-000000000003")
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











