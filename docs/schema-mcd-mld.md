# Schema MCD et MLD complet - MangoTaika

Source: reconstruction a partir de [AppDbContext](../Data/AppDbContext.cs), des entites `Data/Entities/*.cs`, du snapshot EF Core et des migrations recentes.

## Portee

- Le **MCD** donne la vue metier et les grandes cardinalites.
- Le **MLD** donne la vue relationnelle attendue cote EF Core / PostgreSQL.
- Le schema couvre maintenant les sous-domaines historiques **et** les modules ajoutes plus recemment: inscriptions annuelles, parcours scout, programmes annuels, rapports d'activite, propositions de maitrise et cotisations nationales.
- Le dictionnaire de donnees detaille est dans [dictionnaire-donnees.md](./dictionnaire-donnees.md).

## Domaines couverts

- territoire scout et identites
- parcours scout et conformite annuelle
- activites, demandes et gouvernance
- support, finance et cotisations nationales
- LMS / formation
- communication et portail public
- tables techniques ASP.NET Identity

## MCD global

```mermaid
erDiagram
    GROUPES ||--o{ BRANCHES : contient
    GROUPES ||--o{ SCOUTS : rattache
    GROUPES ||--o{ UTILISATEURS : affecte
    BRANCHES ||--o{ SCOUTS : organise
    SCOUTS }o--o{ PARENTS : est_lie_a
    SCOUTS ||--o{ COMPETENCES : possede
    SCOUTS ||--o{ HISTORIQUE_FONCTIONS : historise
    SCOUTS ||--o{ SUIVIS_ACADEMIQUES : suit
    SCOUTS ||--o{ ETAPES_PARCOURS_SCOUT : progresse_dans
    SCOUTS ||--o{ INSCRIPTIONS_ANNUELLES_SCOUT : renouvelle
    SCOUTS ||--o{ TRANSACTIONS_FINANCIERES : cotise

    GROUPES ||--o{ PROGRAMMES_ANNUELS : planifie
    GROUPES ||--o{ PROPOSITIONS_MAITRISE_ANNUELLES : propose
    ACTIVITES ||--|| RAPPORTS_ACTIVITE : cloture_par

    UTILISATEURS ||--o{ ACTIVITES : cree
    GROUPES ||--o{ ACTIVITES : porte
    ACTIVITES ||--o{ DOCUMENTS_ACTIVITE : contient
    ACTIVITES ||--o{ PARTICIPANTS_ACTIVITE : recoit
    ACTIVITES ||--o{ COMMENTAIRES_ACTIVITE : journalise
    SCOUTS ||--o{ PARTICIPANTS_ACTIVITE : participe

    UTILISATEURS ||--o{ DEMANDES_AUTORISATION : soumet
    GROUPES ||--o{ DEMANDES_AUTORISATION : concerne
    DEMANDES_AUTORISATION ||--o{ SUIVIS_DEMANDE : trace
    UTILISATEURS ||--o{ DEMANDES_GROUPE : traite

    SUPPORT_CATALOGUE_SERVICES ||--o{ TICKETS : qualifie
    TICKETS ||--o{ MESSAGES_TICKET : contient
    TICKETS ||--o{ TICKET_PIECES_JOINTES : attache
    TICKETS ||--o{ HISTORIQUES_TICKET : historise
    UTILISATEURS ||--o{ NOTIFICATIONS_UTILISATEUR : recoit

    GROUPES ||--o{ PROJETS_AGR : porte
    PROJETS_AGR ||--o{ TRANSACTIONS_FINANCIERES : genere
    COTISATIONS_NATIONALES_IMPORTS ||--o{ COTISATIONS_NATIONALES_IMPORT_LIGNES : detaille
    SCOUTS ||--o{ COTISATIONS_NATIONALES_IMPORT_LIGNES : rapproche

    FORMATIONS ||--o{ MODULES_FORMATION : contient
    MODULES_FORMATION ||--o{ LECONS : contient
    MODULES_FORMATION ||--|| QUIZZES : porte
    QUIZZES ||--o{ QUESTIONS_QUIZ : contient
    QUESTIONS_QUIZ ||--o{ REPONSES_QUIZ : propose
    FORMATIONS ||--o{ SESSIONS_FORMATION : publie
    FORMATIONS ||--o{ INSCRIPTIONS_FORMATION : ouvre
    SCOUTS ||--o{ INSCRIPTIONS_FORMATION : suit
    SCOUTS ||--o{ PROGRESSIONS_LECON : valide
    SCOUTS ||--o{ TENTATIVES_QUIZ : tente
    FORMATIONS ||--o{ CERTIFICATIONS_FORMATION : delivre
    FORMATIONS ||--o{ JALONS_FORMATION : planifie
    FORMATIONS ||--o{ ANNONCES_FORMATION : publie
    FORMATIONS ||--o{ DISCUSSIONS_FORMATION : anime
    DISCUSSIONS_FORMATION ||--o{ MESSAGES_DISCUSSION_FORMATION : contient

    UTILISATEURS ||--o{ ACTUALITES : publie
    PARTENAIRES ||--o{ LIENS_RESEAUX_SOCIAUX : expose
```

## MLD - coeur scout, territoire et identites

```mermaid
erDiagram
    AspNetUsers {
        Guid Id PK
        string UserName
        string Email
        string PhoneNumber
        bool TwoFactorEnabled
        string Nom
        string Prenom
        string PhotoUrl
        string Matricule
        bool IsActive
        DateTime DateCreation
        Guid GroupeId FK
        Guid BrancheId FK
    }

    Groupes {
        Guid Id PK
        string Nom
        string NomNormalise UK
        string Description
        string Adresse
        string NomChefGroupe
        string LogoUrl
        double Latitude
        double Longitude
        bool IsActive
        Guid ResponsableId FK
    }

    Branches {
        Guid Id PK
        string Nom
        string NomNormalise UK
        string Description
        string LogoUrl
        int AgeMin
        int AgeMax
        string NomChefUnite
        bool IsActive
        Guid GroupeId FK
        Guid ChefUniteId FK
    }

    Scouts {
        Guid Id PK
        string Matricule UK
        string Nom
        string Prenom
        DateTime DateNaissance
        string Sexe
        string Telephone
        string Email
        string PhotoUrl
        string NumeroCarte UK
        string Fonction
        string StatutASCCI
        bool AssuranceAnnuelle
        bool IsActive
        Guid UserId FK
        Guid GroupeId FK
        Guid BrancheId FK
    }

    Parents {
        Guid Id PK
        string Nom
        string Prenom
        string Telephone
        string Email
        string Relation
    }

    ParentScout {
        Guid ParentsId PK_FK
        Guid ScoutsId PK_FK
    }

    Competences {
        Guid Id PK
        string Nom
        string Description
        DateTime DateObtention
        string Niveau
        Guid ScoutId FK
    }

    HistoriqueFonctions {
        Guid Id PK
        string Fonction
        DateTime DateDebut
        DateTime DateFin
        string Commentaire
        Guid ScoutId FK
        Guid UserId FK
        Guid GroupeId FK
    }

    SuivisAcademiques {
        Guid Id PK
        string AnneeScolaire
        string Etablissement
        string NiveauScolaire
        string Classe
        double MoyenneGenerale
        string Mention
        bool EstRedoublant
        Guid ScoutId FK
    }

    CodesInvitation {
        Guid Id PK
        string Code UK
        bool EstUtilise
        DateTime DateCreation
        DateTime DateUtilisation
        Guid CreateurId FK
        Guid UtilisePaId FK
    }

    Groupes ||--o{ Branches : contient
    Groupes ||--o{ Scouts : rattache
    Groupes ||--o{ AspNetUsers : affecte
    Branches ||--o{ Scouts : organise
    AspNetUsers ||--o{ Groupes : responsable_de
    Scouts ||--o{ Branches : chef_unite_de
    Scouts ||--o{ Competences : possede
    Scouts ||--o{ HistoriqueFonctions : historise
    Scouts ||--o{ SuivisAcademiques : suit
    Scouts ||--o{ ParentScout : jointure
    Parents ||--o{ ParentScout : jointure
    AspNetUsers ||--o{ CodesInvitation : cree
    AspNetUsers ||--o{ CodesInvitation : utilise
```

## MLD - parcours scout et conformite annuelle

```mermaid
erDiagram
    Scouts {
        Guid Id PK
        string Matricule UK
        string Nom
        string Prenom
        Guid GroupeId FK
        Guid BrancheId FK
        Guid UserId FK
    }

    Groupes {
        Guid Id PK
        string Nom
    }

    Branches {
        Guid Id PK
        string Nom
        Guid GroupeId FK
    }

    Activites {
        Guid Id PK
        string Titre
        Guid GroupeId FK
        Guid CreateurId FK
    }

    TransactionsFinancieres {
        Guid Id PK
        string Libelle
        decimal Montant
        DateTime DateTransaction
        Guid GroupeId FK
        Guid ActiviteId FK
        Guid ProjetAGRId FK
        Guid ScoutId FK
        Guid CreateurId FK
    }

    EtapesParcoursScouts {
        Guid Id PK
        Guid ScoutId FK
        string NomEtape
        string CodeEtape
        int OrdreAffichage
        DateTime DateValidation
        DateTime DatePrevisionnelle
        string Observations
        bool EstObligatoire
    }

    InscriptionsAnnuellesScouts {
        Guid Id PK
        Guid ScoutId FK
        Guid GroupeId FK
        Guid BrancheId FK
        string FonctionSnapshot
        int AnneeReference
        string LibelleAnnee
        DateTime DateInscription
        DateTime DateValidation
        int Statut
        bool InscriptionParoissialeValidee
        bool CotisationNationaleAjour
        string Observations
        Guid ValideParId FK
    }

    ProgrammesAnnuels {
        Guid Id PK
        Guid GroupeId FK
        int AnneeReference
        string Titre
        string Objectifs
        string CalendrierSynthese
        string Observations
        int Statut
        string CommentaireValidation
        DateTime DateCreation
        DateTime DateSoumission
        DateTime DateValidation
        Guid CreateurId FK
        Guid ValideurId FK
    }

    RapportsActivite {
        Guid Id PK
        Guid ActiviteId FK
        string ResumeExecutif
        string ResultatsObtenus
        string DifficultesRencontrees
        string Recommandations
        string ObservationsComplementaires
        int Statut
        string CommentaireValidation
        DateTime DateCreation
        DateTime DateSoumission
        DateTime DateValidation
        Guid CreateurId FK
        Guid ValideurId FK
    }

    PropositionsMaitriseAnnuelles {
        Guid Id PK
        Guid GroupeId FK
        int AnneeReference
        string Titre
        string CompositionProposee
        string ObjectifsPedagogiques
        string BesoinsFormation
        string Observations
        int Statut
        string CommentaireValidation
        DateTime DateCreation
        DateTime DateSoumission
        DateTime DateValidation
        Guid CreateurId FK
        Guid ValideurId FK
    }

    CotisationsNationalesImports {
        Guid Id PK
        int AnneeReference
        string NomFichier
        DateTime DateImport
        decimal MontantTotal
        int NombreAjour
        int NombreNonAjour
        int NombreAVerifier
        Guid CreateurId FK
    }

    CotisationsNationalesImportLignes {
        Guid Id PK
        Guid ImportId FK
        Guid ScoutId FK
        string Matricule
        string NomImporte
        decimal Montant
        int Statut
        string Motif
    }

    Scouts ||--o{ EtapesParcoursScouts : progresse
    Scouts ||--o{ InscriptionsAnnuellesScouts : renouvelle
    Groupes ||--o{ InscriptionsAnnuellesScouts : snapshot_groupe
    Branches ||--o{ InscriptionsAnnuellesScouts : snapshot_branche
    Groupes ||--o{ ProgrammesAnnuels : planifie
    Groupes ||--o{ PropositionsMaitriseAnnuelles : propose
    Activites ||--|| RapportsActivite : cloture
    CotisationsNationalesImports ||--o{ CotisationsNationalesImportLignes : contient
    Scouts ||--o{ CotisationsNationalesImportLignes : rapproche
    Scouts ||--o{ TransactionsFinancieres : cotise
```

## MLD - activites, demandes, support et finance

```mermaid
erDiagram
    Activites {
        Guid Id PK
        string Titre
        string Description
        DateTime DateDebut
        DateTime DateFin
        string Lieu
        decimal BudgetPrevisionnel
        string NomResponsable
        Guid CreateurId FK
        Guid GroupeId FK
    }

    DocumentsActivite {
        Guid Id PK
        string NomFichier
        string CheminFichier
        string TypeDocument
        DateTime DateUpload
        Guid ActiviteId FK
    }

    ParticipantsActivite {
        Guid Id PK
        Guid ActiviteId FK
        Guid ScoutId FK
        DateTime DateInscription
    }

    CommentairesActivite {
        Guid Id PK
        Guid ActiviteId FK
        Guid AuteurId FK
        string Contenu
        string TypeAction
        DateTime DateCreation
    }

    DemandesAutorisation {
        Guid Id PK
        string Titre
        string Description
        DateTime DateActivite
        DateTime DateFin
        string Lieu
        int NombreParticipants
        string Budget
        string TdrContenu
        string MotifRejet
        DateTime DateCreation
        DateTime DateValidation
        Guid DemandeurId FK
        Guid ValideurId FK
        Guid GroupeId FK
    }

    SuivisDemande {
        Guid Id PK
        Guid DemandeId FK
        string Commentaire
        string Auteur
        DateTime Date
    }

    DemandesGroupe {
        Guid Id PK
        string NomGroupe
        string Commune
        string Quartier
        string NomResponsable
        string TelephoneResponsable
        string EmailResponsable
        int NombreMembresPrevus
        string Motivation
        Guid TraiteParId FK
    }

    SupportCatalogueServices {
        Guid Id PK
        string Code UK
        string Nom
        string Description
        int DelaiSlaHeures
        Guid AssigneParDefautId FK
        Guid GroupeParDefautId FK
        Guid AuteurId FK
        bool EstActif
    }

    Tickets {
        Guid Id PK
        string NumeroTicket
        string Sujet
        string Description
        DateTime DateCreation
        DateTime DateLimiteSla
        DateTime DatePremiereReponse
        DateTime DateAffectation
        DateTime DateResolution
        bool EstEscalade
        int NiveauEscalade
        int NoteSatisfaction
        Guid ServiceCatalogueId FK
        Guid CreateurId FK
        Guid AssigneAId FK
        Guid GroupeAssigneId FK
    }

    MessagesTicket {
        Guid Id PK
        Guid TicketId FK
        Guid AuteurId FK
        string Contenu
        bool EstNoteInterne
        DateTime DateEnvoi
    }

    TicketPiecesJointes {
        Guid Id PK
        Guid TicketId FK
        Guid AjouteParId FK
        string NomOriginal
        string UrlFichier
        string TypeMime
        long TailleOctets
        DateTime DateAjout
    }

    HistoriquesTicket {
        Guid Id PK
        Guid TicketId FK
        Guid AuteurId FK
        string Commentaire
        DateTime DateChangement
    }

    NotificationsUtilisateur {
        Guid Id PK
        Guid UserId FK
        string Titre
        string Message
        string Categorie
        string Lien
        bool EstLue
        DateTime DateCreation
        DateTime DateLecture
    }

    ProjetsAGR {
        Guid Id PK
        string Nom
        string Description
        decimal BudgetInitial
        DateTime DateDebut
        DateTime DateFin
        string Responsable
        Guid GroupeId FK
        Guid CreateurId FK
    }

    TransactionsFinancieres {
        Guid Id PK
        string Libelle
        decimal Montant
        DateTime DateTransaction
        string Reference
        string Commentaire
        Guid GroupeId FK
        Guid ActiviteId FK
        Guid ProjetAGRId FK
        Guid ScoutId FK
        Guid CreateurId FK
    }

    Groupes ||--o{ Activites : porte
    Activites ||--o{ DocumentsActivite : contient
    Activites ||--o{ ParticipantsActivite : recoit
    Activites ||--o{ CommentairesActivite : journalise
    Scouts ||--o{ ParticipantsActivite : participe
    DemandesAutorisation ||--o{ SuivisDemande : trace
    Groupes ||--o{ DemandesAutorisation : concerne
    AspNetUsers ||--o{ DemandesAutorisation : demande_valide
    AspNetUsers ||--o{ DemandesGroupe : traite
    SupportCatalogueServices ||--o{ Tickets : qualifie
    Tickets ||--o{ MessagesTicket : contient
    Tickets ||--o{ TicketPiecesJointes : attache
    Tickets ||--o{ HistoriquesTicket : historise
    AspNetUsers ||--o{ NotificationsUtilisateur : recoit
    Groupes ||--o{ ProjetsAGR : porte
    ProjetsAGR ||--o{ TransactionsFinancieres : genere
    Activites ||--o{ TransactionsFinancieres : finance
    Scouts ||--o{ TransactionsFinancieres : concerne
```

## MLD - LMS / formation

```mermaid
erDiagram
    Formations {
        Guid Id PK
        string Titre
        string Description
        string ImageUrl
        int Niveau
        int Statut
        int DureeEstimeeHeures
        DateTime DateCreation
        DateTime DatePublication
        bool DelivreBadge
        bool DelivreAttestation
        bool DelivreCertificat
        Guid BrancheCibleId FK
        Guid CompetenceLieeId
        Guid AuteurId FK
    }

    FormationsPrerequis {
        Guid FormationId PK_FK
        Guid PrerequisFormationId PK_FK
    }

    ModulesFormation {
        Guid Id PK
        string Titre
        string Description
        int Ordre
        Guid FormationId FK
    }

    Lecons {
        Guid Id PK
        string Titre
        string ContenuTexte
        string VideoUrl
        string DocumentUrl
        int Ordre
        int DureeMinutes
        Guid ModuleId FK
    }

    Quizzes {
        Guid Id PK
        string Titre
        int NoteMinimale
        int NombreTentativesMax
        DateTime DateOuvertureDisponibilite
        DateTime DateFermetureDisponibilite
        Guid ModuleId FK
    }

    QuestionsQuiz {
        Guid Id PK
        string Enonce
        int Ordre
        Guid QuizId FK
    }

    ReponsesQuiz {
        Guid Id PK
        string Texte
        bool EstCorrecte
        int Ordre
        Guid QuestionId FK
    }

    SessionsFormation {
        Guid Id PK
        string Titre
        string Description
        bool EstSelfPaced
        bool EstPubliee
        DateTime DateOuverture
        DateTime DateFermeture
        Guid FormationId FK
    }

    InscriptionsFormation {
        Guid Id PK
        DateTime DateInscription
        DateTime DateTerminee
        int ProgressionPourcent
        Guid ScoutId FK
        Guid FormationId FK
        Guid SessionFormationId FK
    }

    ProgressionsLecon {
        Guid Id PK
        bool EstTerminee
        DateTime DateTerminee
        Guid ScoutId FK
        Guid LeconId FK
    }

    TentativesQuiz {
        Guid Id PK
        int Score
        bool Reussi
        DateTime DateTentative
        string ReponsesJson
        Guid ScoutId FK
        Guid QuizId FK
    }

    CertificationsFormation {
        Guid Id PK
        string Code UK
        DateTime DateEmission
        int ScoreFinal
        string Mention
        Guid ScoutId FK
        Guid FormationId FK
        Guid InscriptionFormationId FK
    }

    JalonsFormation {
        Guid Id PK
        Guid FormationId FK
        string Titre
        string Description
        DateTime DateJalon
        bool EstPublie
    }

    AnnoncesFormation {
        Guid Id PK
        string Titre
        string Contenu
        bool EstPubliee
        DateTime DatePublication
        Guid FormationId FK
        Guid AuteurId FK
    }

    DiscussionsFormation {
        Guid Id PK
        string Titre
        string ContenuInitial
        DateTime DateCreation
        DateTime DateDerniereActivite
        bool EstVerrouillee
        Guid FormationId FK
        Guid AuteurId FK
    }

    MessagesDiscussionFormation {
        Guid Id PK
        string Contenu
        DateTime DateCreation
        bool EstSupprime
        Guid DiscussionFormationId FK
        Guid AuteurId FK
    }

    Formations ||--o{ ModulesFormation : contient
    Formations ||--o{ SessionsFormation : publie
    Formations ||--o{ InscriptionsFormation : ouvre
    Formations ||--o{ CertificationsFormation : delivre
    Formations ||--o{ JalonsFormation : planifie
    Formations ||--o{ AnnoncesFormation : publie
    Formations ||--o{ DiscussionsFormation : anime
    Formations ||--o{ FormationsPrerequis : prerequis_de
    Formations ||--o{ FormationsPrerequis : debloquee_par
    Branches ||--o{ Formations : cible
    AspNetUsers ||--o{ Formations : auteur
    ModulesFormation ||--o{ Lecons : contient
    ModulesFormation ||--|| Quizzes : porte
    Quizzes ||--o{ QuestionsQuiz : contient
    QuestionsQuiz ||--o{ ReponsesQuiz : propose
    Scouts ||--o{ InscriptionsFormation : suit
    SessionsFormation ||--o{ InscriptionsFormation : cadre
    Scouts ||--o{ ProgressionsLecon : valide
    Lecons ||--o{ ProgressionsLecon : est_suivie
    Scouts ||--o{ TentativesQuiz : tente
    Quizzes ||--o{ TentativesQuiz : est_tente
    Scouts ||--o{ CertificationsFormation : recoit
    InscriptionsFormation ||--o{ CertificationsFormation : justifie
    DiscussionsFormation ||--o{ MessagesDiscussionFormation : contient
```

## MLD - communication, vitrine et contenus

```mermaid
erDiagram
    Actualites {
        Guid Id PK
        string Titre
        string Resume
        string Contenu
        string ImageUrl
        DateTime DatePublication
        bool EstPublie
        Guid CreateurId FK
    }

    Galeries {
        Guid Id PK
        string Titre
        string Description
        string CheminMedia
        string TypeMedia
        DateTime DateUpload
        bool EstPublie
    }

    MotsCommissaire {
        Guid Id PK
        string Titre
        string Contenu
        string PhotoUrl
        int Annee
        bool EstActif
    }

    LivreDor {
        Guid Id PK
        string NomAuteur
        string Message
        DateTime DateCreation
        bool EstValide
        DateTime DateValidation
    }

    ContactMessages {
        Guid Id PK
        string Nom
        string Email
        string Sujet
        string Message
        string Type
        DateTime DateEnvoi
    }

    MembresHistoriques {
        Guid Id PK
        string Nom
        string PhotoUrl
        string Description
        string Periode
        int Ordre
    }

    Partenaires {
        Guid Id PK
        string Nom
        string Description
        string LogoUrl
        string SiteWeb
        string TypePartenariat
        int Ordre
    }

    LiensReseauxSociaux {
        Guid Id PK
        string Plateforme
        string Url
        string Icone
        int Ordre
        bool EstActif
    }

    AspNetUsers ||--o{ Actualites : publie
```

## Tables techniques ASP.NET Identity

Le modele physique comprend aussi les tables techniques suivantes, inheritees de `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `AspNetRoleClaims`

## Contraintes et regles notables

- unicite de `Scouts.Matricule`
- unicite optionnelle de `Scouts.NumeroCarte`
- unicite logique des groupes actifs via `Groupes.NomNormalise`
- unicite logique des branches actives par groupe via `(Branches.GroupeId, Branches.NomNormalise)`
- unicite de `CodesInvitation.Code`
- unicite de `SupportCatalogueServices.Code`
- unicite `(ActiviteId, ScoutId)` sur `ParticipantsActivite`
- unicite `(ScoutId, AnneeReference)` sur `InscriptionsAnnuellesScouts`
- unicite `(GroupeId, AnneeReference)` sur `ProgrammesAnnuels`
- unicite `(GroupeId, AnneeReference)` sur `PropositionsMaitriseAnnuelles`
- unicite `RapportsActivite.ActiviteId`
- index `(ImportId, Matricule)` sur `CotisationsNationalesImportLignes`
- unicite `(ScoutId, FormationId)` sur `InscriptionsFormation`
- unicite `(ScoutId, LeconId)` sur `ProgressionsLecon`
- unicite `CertificationsFormation.Code`
- unicite `(ScoutId, FormationId, Type)` sur `CertificationsFormation`

## Remarques de lecture

- Le district est opere fonctionnellement via la logique metier autour du groupe `Equipe de District Mango Taika`.
- `CompetenceLieeId` dans `Formations` existe comme reference logique metier, sans FK explicite dans `OnModelCreating`.
- La relation `Scout <-> Parent` est materialisee physiquement par la table implicite `ParentScout` generee par EF Core.
- Certaines tables ont des logiques de soft delete ou de publication (`IsActive`, `EstActif`, `EstPublie`, `EstSupprime`) qui ne se lisent pas uniquement a travers le MLD.

## Livrables associes

- schema complet en markdown: [schema-mcd-mld.md](./schema-mcd-mld.md)
- dictionnaire detaille: [dictionnaire-donnees.md](./dictionnaire-donnees.md)
- schema editable multi-pages: [schema-mcd-mld.drawio](./schema-mcd-mld.drawio)
- schema SVG regeneres et alignes:
  - [schema-mcd-global.svg](./schema-mcd-global.svg)
  - [schema-mld-coeur-territoire.svg](./schema-mld-coeur-territoire.svg)
  - [schema-mld-parcours-conformite-annuelle.svg](./schema-mld-parcours-conformite-annuelle.svg)
  - [schema-mld-operations-support-finance.svg](./schema-mld-operations-support-finance.svg)
  - [schema-mld-lms-formation.svg](./schema-mld-lms-formation.svg)
  - [schema-mld-communication-vitrine.svg](./schema-mld-communication-vitrine.svg)
- generateurs:
  - [generate_schema_diagrams.py](../scripts/generate_schema_diagrams.py)
  - [generate_data_dictionary.py](../scripts/generate_data_dictionary.py)

Note:
- les fichiers `drawio` et `svg` ci-dessus sont maintenant realignes avec cette version complete du schema.


