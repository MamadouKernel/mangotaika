# Matrice de conformite aux cahiers de charge - MangoTaika

Date de mise a jour: 2026-04-01

## Sources analysees

- `C:\Users\kerne\Downloads\cahier_des_charges_application_scouts.docx`
- `C:\Users\kerne\Downloads\Cahier de charge - Gestion District MANGO TAIKA V1.docx`

## Methode

Cette matrice distingue 2 niveaux:

- **Conformite logicielle**: ce que le code, la base et les ecrans prouvent deja.
- **Conformite projet / contractuelle**: ce qui demande aussi des livrables hors code (documentation, exploitation, formation, recette, validation metier).

## Verification technique actuelle

- `dotnet build --no-restore /p:UseAppHost=false`: OK
- `dotnet test .\MangoTaika.Tests\MangoTaika.Tests.csproj`: OK (`118/118`)
- `dotnet ef database update`: OK
- migrations appliquees:
  - `20260401040932_AddComplianceCoreModules`
  - `20260401043813_AddNationalDuesImportWorkflow`
  - `20260401050503_AddAnnualSnapshotAndScoutJourney`

## Synthese executive

### Verdict logiciel

Sur le **perimetre applicatif du depot**, le projet couvre maintenant l'essentiel des exigences fonctionnelles majeures des cahiers:

- gestion des scouts
- suivi du parcours scout
- gestion administrative des activites
- gestion de la maitrise
- gestion du programme annuel
- suivi des cotisations nationales
- fonctionnement niveau groupe et district Mango Taika

### Verdict contractuel global

Le projet ne peut **pas encore etre certifie a 100% conforme aux cahiers** au sens strict tant que les elements suivants ne sont pas produits et valides:

- documentation utilisateur
- documentation administrateur / exploitation
- procedure de sauvegarde / restauration / mise en service
- plan ou support de formation des utilisateurs
- recette metier formelle avec validation des parties prenantes
- preuve de deploiement et de fonctionnement en environnement cible selon les exigences du projet

## Matrice de conformite fonctionnelle

| Exigence | Statut | Preuves principales dans le code | Commentaire |
|---|---|---|---|
| Gestion des membres scouts | Conforme logiciel | `Data/Entities/Scout.cs`, `Controllers/ScoutsController.cs`, `Views/Scouts/Index.cshtml` | CRUD, import Excel, recherche, filtres, details, rattachement groupe/branche. |
| Suivi du parcours scout | Conforme logiciel | `Data/Entities/ParcoursScout.cs`, `Controllers/CompetencesController.cs`, `Views/Competences/Progression.cshtml` | Etapes du parcours, dates previsionnelles, validations, observations, vue de progression. |
| Inscriptions annuelles | Conforme logiciel | `Data/Entities/WorkflowMetier.cs`, `Controllers/InscriptionsAnnuellesController.cs`, `Views/InscriptionsAnnuelles/Index.cshtml` | Historique annuel, statut, validation paroissiale, cotisation, snapshot groupe/branche/fonction. |
| Gestion administrative des activites | Conforme logiciel | `Controllers/ActivitesController.cs`, `Views/Activites/Details.cshtml` | Creation, suivi, validation, participants, pieces, cycle d'activite. |
| Rapports d'activite | Conforme logiciel | `Controllers/RapportsActiviteController.cs`, `Views/RapportsActivite/Details.cshtml` | Rapport post-activite, resultats, difficultes, recommandations, validation. |
| Gestion du programme annuel | Conforme logiciel | `Controllers/ProgrammesAnnuelsController.cs`, `Views/ProgrammesAnnuels/Details.cshtml` | Brouillon, soumission, revision, validation du programme annuel. |
| Gestion de la maitrise | Conforme logiciel | `Controllers/PropositionsMaitriseController.cs`, `Controllers/GroupesController.cs`, `Controllers/BranchesController.cs` | Propositions annuelles de maitrise + regles de selection des responsables par fonction. |
| Suivi des cotisations nationales | Conforme logiciel | `Data/Entities/CotisationsNationales.cs`, `Controllers/CotisationsNationalesController.cs`, `Views/CotisationsNationales/Index.cshtml` | Import Excel, rapprochement A jour / Non a jour / A verifier, synchronisation avec inscriptions annuelles et transactions. |
| Niveau Groupe et District | Conforme avec reserve technique | `Controllers/GroupesController.cs`, `Controllers/BranchesController.cs`, `Services/DistrictBranchInheritanceService.cs` | Fonctionnel pour Mango Taika. Reserve: le district est gere par une regle metier specialisee autour du groupe `Equipe de District Mango Taika`, pas par une entite `District` generique. |
| Reporting et exports | Conforme logiciel | `Controllers/ReportingController.cs`, `wwwroot/js/list-export.js`, vues de listes et details | Reporting, export PDF/Excel sur listes et details principaux. |
| Carte / geolocalisation des groupes | Conforme logiciel | `Controllers/GroupesController.cs`, `Views/Groupes/Carte.cshtml` | Cartographie des entites et geocodage. |
| Communication et portail public | Conforme logiciel | `Controllers/HomeController.cs`, `Controllers/MotCommissaireController.cs`, `Controllers/GalerieController.cs`, `Controllers/PartenairesController.cs` | Vitrine publique district, contenus, galerie, partenaires, carte. |
| Support et base de connaissances | Conforme logiciel | `Controllers/TicketsController.cs`, `Controllers/KnowledgeBaseController.cs`, `Controllers/SupportCatalogController.cs` | Centre de support, catalogue, base de connaissances. |
| Formation / LMS | Conforme logiciel | `Controllers/FormationsController.cs`, `Data/AppDbContext.cs` | Formations, modules, quiz, inscriptions, statistiques. |
| Gestion financiere / AGR | Conforme logiciel | `Controllers/FinancesController.cs`, `Controllers/AGRController.cs` | Transactions, categories, projets AGR. |
| Authentification, roles et securite de base | Conforme logiciel | `Controllers/AccountController.cs`, `Models/RegisterViewModel.cs` | Comptes, roles, consentement, 2FA SMS deja present dans l'application. |

## Matrice de conformite projet / contractuelle

| Exigence projet | Statut actuel | Preuves / constats | Action restante |
|---|---|---|---|
| Documentation utilisateur | A produire | Pas de manuel utilisateur structure livre dans le depot | Rediger un guide utilisateur par profil (gestionnaire, administrateur, consultant, etc.). |
| Documentation d'administration / exploitation | A produire | Pas de runbook complet d'exploitation dans le depot | Rediger installation, configuration, sauvegarde, restauration, supervision, rotation des secrets. |
| Procedure de mise en production | A produire / formaliser | Le code et les migrations existent, mais pas de dossier d'exploitation formalise | Produire la procedure de deploiement, rollback et verification post-deploiement. |
| Sauvegarde / restauration | A produire | Pas de preuve documentaire ou script d'exploitation formalise dans le depot | Definir RPO/RTO, sauvegardes PostgreSQL, restoration testee, responsabilites. |
| Formation des utilisateurs | A produire | Aucune preuve de support de formation ou planning d'accompagnement | Produire support de formation, plan de prise en main et feuille de presence / validation. |
| Recette metier formelle | A produire | Les tests techniques passent, mais cela ne remplace pas un PV de recette metier | Construire une campagne UAT et faire valider les cas par les acteurs metier. |
| Validation contractuelle finale | Non prouvee | Aucune signature / validation des parties prenantes dans le depot | Obtenir validation officielle du commanditaire sur la version livree. |

## Points forts constates

- La base de code est coherentement testee (`118/118`).
- Les migrations sont appliquees et la base est alignee avec le code.
- Les principaux ecarts fonctionnels identifies initialement ont ete couverts dans le logiciel.
- Le projet possede maintenant une couverture metier plus large que le noyau initial (cotisations, programmes annuels, rapports d'activite, maitrise, parcours scout, inscriptions annuelles).

## Reserves a garder en tete

- La conformite **logicielle** est tres elevee, mais la conformite **contractuelle globale** depend encore de livrables hors code.
- La gestion du district est fonctionnelle pour Mango Taika, mais reste specialisee metier plutot que totalement generique dans le modele.
- Le fait que les tests passent ne constitue pas a lui seul une recette utilisateur ou une homologation projet.

## Conclusion

### Reponse courte

- **Oui**, le projet est maintenant tres proche d'une conformite complete sur le **plan logiciel fonctionnel**.
- **Non**, il n'est pas encore possible d'affirmer honnetement **100% conforme aux cahiers** au sens **projet / contractuel global** sans les livrables et validations hors code cites plus haut.

### Formulation recommandee

> L'application est desormais conforme sur le perimetre logiciel fonctionnel majeur des cahiers de charge, avec build, tests et migrations valides. La conformite a 100% au sens contractuel reste subordonnee a la production des livrables d'exploitation, de documentation, de formation et a la recette metier formelle.
