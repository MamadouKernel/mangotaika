# Dictionnaire de donnees - MangoTaika

Date de generation: 2026-04-01

## Sources

- `Data/AppDbContext.cs`
- `Data/Entities/*.cs`
- conventions EF Core et ASP.NET Identity du projet

## Legende

- `PK` : cle primaire
- `FK` : cle etrangere
- `Enum` : champ base sur une enumeration C#
- `Flag` : booleen de statut
- `Nullable` : accepte ou non les valeurs nulles dans le modele

## Territoire, identites et parcours scout

### `AspNetUsers`

Role: Comptes applicatifs et administratifs, avec extensions metier MangoTaika.
Source: `Data/Entities/ApplicationUser.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `UserName` | `string?` | Oui | Metier | Champ metier de la table. |
| `NormalizedUserName` | `string?` | Oui | Metier | Champ metier de la table. |
| `Email` | `string?` | Oui | Metier | Champ metier de la table. |
| `NormalizedEmail` | `string?` | Oui | Metier | Champ metier de la table. |
| `EmailConfirmed` | `bool` | Non | Flag | Champ metier de la table. |
| `PasswordHash` | `string?` | Oui | Metier | Champ metier de la table. |
| `PhoneNumber` | `string?` | Oui | Metier | Champ metier de la table. |
| `PhoneNumberConfirmed` | `bool` | Non | Flag | Champ metier de la table. |
| `TwoFactorEnabled` | `bool` | Non | Flag | Champ metier de la table. |
| `LockoutEnd` | `DateTime?` | Oui | Date | Champ metier de la table. |
| `LockoutEnabled` | `bool` | Non | Flag | Champ metier de la table. |
| `AccessFailedCount` | `int` | Non | Metier | Champ metier de la table. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Prenom` | `string` | Non | Metier | Champ metier de la table. |
| `PhotoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Matricule` | `string?` | Oui | Metier | Champ metier de la table. |
| `IsActive` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `BrancheId` | `Guid?` | Oui | FK | Reference vers `Branches`. |

### `Groupes`

Role: Entites scoutes du district, y compris l'equipe de district.
Source: `Data/Entities/Groupe.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `NomNormalise` | `string` | Non | Metier | Champ metier de la table. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `LogoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Latitude` | `double?` | Oui | Metier | Champ metier de la table. |
| `Longitude` | `double?` | Oui | Metier | Champ metier de la table. |
| `Adresse` | `string?` | Oui | Metier | Champ metier de la table. |
| `NomChefGroupe` | `string?` | Oui | Metier | Champ metier de la table. |
| `IsActive` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `ResponsableId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- NomNormalise unique sur les groupes actifs.

### `Branches`

Role: Unites ou branches d'age rattachees a un groupe.
Source: `Data/Entities/Branche.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `NomNormalise` | `string` | Non | Metier | Champ metier de la table. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `LogoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `AgeMin` | `int?` | Oui | Metier | Champ metier de la table. |
| `AgeMax` | `int?` | Oui | Metier | Champ metier de la table. |
| `NomChefUnite` | `string?` | Oui | Metier | Champ metier de la table. |
| `ChefUniteId` | `Guid?` | Oui | FK | Reference vers `Scouts`. |
| `IsActive` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `GroupeId` | `Guid` | Non | FK | Reference vers `Groupes`. |

Contraintes / notes:
- Unicite logique du nom dans un groupe actif via (GroupeId, NomNormalise).

### `Scouts`

Role: Fiches centrales des membres scouts et responsables terrain.
Source: `Data/Entities/Scout.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Matricule` | `string` | Non | Metier | Champ metier de la table. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Prenom` | `string` | Non | Metier | Champ metier de la table. |
| `DateNaissance` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `LieuNaissance` | `string?` | Oui | Metier | Champ metier de la table. |
| `Sexe` | `string?` | Oui | Metier | Champ metier de la table. |
| `PhotoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Telephone` | `string?` | Oui | Metier | Champ metier de la table. |
| `Email` | `string?` | Oui | Metier | Champ metier de la table. |
| `RegionScoute` | `string?` | Oui | Metier | Champ metier de la table. |
| `District` | `string?` | Oui | Metier | Champ metier de la table. |
| `NumeroCarte` | `string?` | Oui | Metier | Champ metier de la table. |
| `Fonction` | `string?` | Oui | Metier | Champ metier de la table. |
| `StatutASCCI` | `string?` | Oui | Metier | Champ metier de la table. |
| `AssuranceAnnuelle` | `bool` | Non | Metier | Champ metier de la table. |
| `AdresseGeographique` | `string?` | Oui | Metier | Champ metier de la table. |
| `IsActive` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateInscription` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `UserId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `BrancheId` | `Guid?` | Oui | FK | Reference vers `Branches`. |

Contraintes / notes:
- Matricule unique.
- NumeroCarte unique si renseigne.

### `Parents`

Role: Representants ou contacts parentaux relies aux scouts.
Source: `Data/Entities/Parent.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Prenom` | `string` | Non | Metier | Champ metier de la table. |
| `Telephone` | `string?` | Oui | Metier | Champ metier de la table. |
| `Email` | `string?` | Oui | Metier | Champ metier de la table. |
| `Relation` | `string?` | Oui | Metier | Champ metier de la table. |

### `ParentScout`

Role: Table de jointure many-to-many entre parents et scouts.
Source: `Association implicite EF Core Scout <-> Parent`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `ParentsId` | `Guid` | Non | PK/FK | Champ metier de la table. |
| `ScoutsId` | `Guid` | Non | PK/FK | Champ metier de la table. |

### `Competences`

Role: Competences et acquis du scout.
Source: `Data/Entities/Competence.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `DateObtention` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `Niveau` | `string?` | Oui | Metier | Champ metier de la table. |
| `Type` | `TypeCompetence` | Non | Enum | Champ metier de la table. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |

### `HistoriqueFonctions`

Role: Historisation des fonctions exercees par un scout ou un utilisateur.
Source: `Data/Entities/HistoriqueFonction.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Fonction` | `string` | Non | Metier | Champ metier de la table. |
| `DateDebut` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateFin` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `Commentaire` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `ScoutId` | `Guid?` | Oui | FK | Reference vers `Scouts`. |
| `UserId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |

### `SuivisAcademiques`

Role: Suivi scolaire et academique du scout.
Source: `Data/Entities/SuiviAcademique.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `AnneeScolaire` | `string` | Non | Metier | Champ metier de la table. |
| `Etablissement` | `string?` | Oui | Metier | Champ metier de la table. |
| `NiveauScolaire` | `string` | Non | Metier | Champ metier de la table. |
| `Classe` | `string?` | Oui | Metier | Champ metier de la table. |
| `MoyenneGenerale` | `double?` | Oui | Metier | Champ metier de la table. |
| `Mention` | `string?` | Oui | Metier | Champ metier de la table. |
| `Observations` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `EstRedoublant` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateSaisie` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |

### `CodesInvitation`

Role: Codes d'invitation utilises pour l'inscription ou l'activation de comptes.
Source: `Data/Entities/CodeInvitation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Code` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `EstUtilise` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateUtilisation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `UtilisePaId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- Code unique.

### `EtapesParcoursScouts`

Role: Etapes du parcours scout, avec prevision et validation.
Source: `Data/Entities/ParcoursScout.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `NomEtape` | `string` | Non | Metier | Champ metier de la table. |
| `CodeEtape` | `string?` | Oui | Metier | Champ metier de la table. |
| `OrdreAffichage` | `int` | Non | Metier | Champ metier de la table. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DatePrevisionnelle` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `Observations` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `EstObligatoire` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |

### `InscriptionsAnnuellesScouts`

Role: Historique annuel des inscriptions et de la conformite du scout.
Source: `Data/Entities/WorkflowMetier.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `BrancheId` | `Guid?` | Oui | FK | Reference vers `Branches`. |
| `FonctionSnapshot` | `string?` | Oui | Metier | Champ metier de la table. |
| `AnneeReference` | `int` | Non | Metier | Champ metier de la table. |
| `LibelleAnnee` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `DateInscription` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `Statut` | `StatutInscriptionAnnuelle` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `InscriptionParoissialeValidee` | `bool` | Non | Metier | Champ metier de la table. |
| `CotisationNationaleAjour` | `bool` | Non | Metier | Champ metier de la table. |
| `Observations` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `ValideParId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- Unicite (ScoutId, AnneeReference).

## Activites, gouvernance et workflows

### `Activites`

Role: Activites scouts soumises, suivies et validees.
Source: `Data/Entities/Activite.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Type` | `TypeActivite` | Non | Enum | Champ metier de la table. |
| `DateDebut` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateFin` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `Lieu` | `string?` | Oui | Metier | Champ metier de la table. |
| `BudgetPrevisionnel` | `decimal?` | Oui | Metier | Champ metier de la table. |
| `NomResponsable` | `string?` | Oui | Metier | Champ metier de la table. |
| `Statut` | `StatutActivite` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `MotifRejet` | `string?` | Oui | Metier | Champ metier de la table. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |

### `DocumentsActivite`

Role: Documents rattaches a une activite.
Source: `Data/Entities/DocumentActivite.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `NomFichier` | `string` | Non | Metier | Champ metier de la table. |
| `CheminFichier` | `string` | Non | Metier | Chemin, URL ou ressource associee. |
| `TypeDocument` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateUpload` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `ActiviteId` | `Guid` | Non | FK | Reference vers `Activites`. |

### `ParticipantsActivite`

Role: Participants scouts inscrits a une activite.
Source: `Data/Entities/ParticipantActivite.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `ActiviteId` | `Guid` | Non | FK | Reference vers `Activites`. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `Presence` | `StatutPresence` | Non | Enum | Champ metier de la table. |
| `DateInscription` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

Contraintes / notes:
- Unicite (ActiviteId, ScoutId).

### `CommentairesActivite`

Role: Commentaires, actions et journal d'une activite.
Source: `Data/Entities/CommentaireActivite.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `ActiviteId` | `Guid` | Non | FK | Reference vers `Activites`. |
| `AuteurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `TypeAction` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `DemandesAutorisation`

Role: Demandes administratives ou autorisations d'activite.
Source: `Data/Entities/DemandeAutorisation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `TypeActivite` | `TypeActiviteDemande` | Non | Enum | Champ metier de la table. |
| `DateActivite` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateFin` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `Lieu` | `string?` | Oui | Metier | Champ metier de la table. |
| `NombreParticipants` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `Objectifs` | `string?` | Oui | Metier | Champ metier de la table. |
| `MoyensLogistiques` | `string?` | Oui | Metier | Champ metier de la table. |
| `Budget` | `string?` | Oui | Metier | Champ metier de la table. |
| `Observations` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `TdrContenu` | `string?` | Oui | Metier | Champ metier de la table. |
| `Statut` | `StatutDemande` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `MotifRejet` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DemandeurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `ValideurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |

### `SuivisDemande`

Role: Journal de suivi associe a une demande d'autorisation.
Source: `Data/Entities/DemandeAutorisation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `DemandeId` | `Guid` | Non | FK | Reference vers `DemandesAutorisation`. |
| `AncienStatut` | `StatutDemande` | Non | Enum | Champ metier de la table. |
| `NouveauStatut` | `StatutDemande` | Non | Enum | Champ metier de la table. |
| `Commentaire` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Auteur` | `string?` | Oui | Metier | Champ metier de la table. |
| `Date` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `DemandesGroupe`

Role: Demandes de creation / reconnaissance d'entite scoute.
Source: `Data/Entities/DemandeGroupe.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `NomGroupe` | `string` | Non | Metier | Champ metier de la table. |
| `Commune` | `string` | Non | Metier | Champ metier de la table. |
| `Quartier` | `string` | Non | Metier | Champ metier de la table. |
| `NomResponsable` | `string` | Non | Metier | Champ metier de la table. |
| `TelephoneResponsable` | `string` | Non | Metier | Champ metier de la table. |
| `EmailResponsable` | `string?` | Oui | Metier | Champ metier de la table. |
| `Motivation` | `string?` | Oui | Metier | Champ metier de la table. |
| `NombreMembresPrevus` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `Statut` | `StatutDemandeGroupe` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `MotifRejet` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateTraitement` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `TraiteParId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

### `ProgrammesAnnuels`

Role: Programme annuel d'un groupe ou du district.
Source: `Data/Entities/WorkflowMetier.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `AnneeReference` | `int` | Non | Metier | Champ metier de la table. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Objectifs` | `string` | Non | Metier | Champ metier de la table. |
| `CalendrierSynthese` | `string` | Non | Metier | Champ metier de la table. |
| `Observations` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Statut` | `StatutWorkflowDocument` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `CommentaireValidation` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateSoumission` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `ValideurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- Unicite (GroupeId, AnneeReference).

### `RapportsActivite`

Role: Rapports post-activite avec validation.
Source: `Data/Entities/WorkflowMetier.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `ActiviteId` | `Guid` | Non | FK | Reference vers `Activites`. |
| `ResumeExecutif` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `ResultatsObtenus` | `string` | Non | Metier | Champ metier de la table. |
| `DifficultesRencontrees` | `string` | Non | Metier | Champ metier de la table. |
| `Recommandations` | `string` | Non | Metier | Champ metier de la table. |
| `ObservationsComplementaires` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Statut` | `StatutWorkflowDocument` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `CommentaireValidation` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateSoumission` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `ValideurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- Un rapport maximum par activite.

### `PropositionsMaitriseAnnuelles`

Role: Propositions annuelles de maitrise / encadrement.
Source: `Data/Entities/WorkflowMetier.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `GroupeId` | `Guid` | Non | FK | Reference vers `Groupes`. |
| `AnneeReference` | `int` | Non | Metier | Champ metier de la table. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `CompositionProposee` | `string` | Non | Metier | Champ metier de la table. |
| `ObjectifsPedagogiques` | `string` | Non | Metier | Champ metier de la table. |
| `BesoinsFormation` | `string?` | Oui | Metier | Champ metier de la table. |
| `Observations` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Statut` | `StatutWorkflowDocument` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `CommentaireValidation` | `string?` | Oui | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateSoumission` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `ValideurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- Unicite (GroupeId, AnneeReference).

## Support, finance et cotisations

### `SupportCatalogueServices`

Role: Catalogue des services du centre de support.
Source: `Data/Entities/Ticket.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Code` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `TypeParDefaut` | `TypeTicket` | Non | Enum | Champ metier de la table. |
| `CategorieParDefaut` | `CategorieTicket` | Non | Enum | Champ metier de la table. |
| `ImpactParDefaut` | `ImpactTicket` | Non | Enum | Champ metier de la table. |
| `UrgenceParDefaut` | `UrgenceTicket` | Non | Enum | Champ metier de la table. |
| `DelaiSlaHeures` | `int` | Non | Metier | Champ metier de la table. |
| `AssigneParDefautId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |
| `GroupeParDefautId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `EstActif` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `AuteurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

Contraintes / notes:
- Code unique.

### `SupportKnowledgeArticles`

Role: Base de connaissances du support.
Source: `Data/Entities/Ticket.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Resume` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `Categorie` | `string` | Non | Metier | Champ metier de la table. |
| `MotsCles` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `EstPublie` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateMiseAJour` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `AuteurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

### `Tickets`

Role: Tickets du centre de support avec SLA, escalade et satisfaction.
Source: `Data/Entities/Ticket.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `NumeroTicket` | `string` | Non | Metier | Champ metier de la table. |
| `Sujet` | `string` | Non | Metier | Champ metier de la table. |
| `Description` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `Type` | `TypeTicket` | Non | Enum | Champ metier de la table. |
| `Categorie` | `CategorieTicket` | Non | Enum | Champ metier de la table. |
| `Impact` | `ImpactTicket` | Non | Enum | Champ metier de la table. |
| `Urgence` | `UrgenceTicket` | Non | Enum | Champ metier de la table. |
| `Priorite` | `PrioriteTicket` | Non | Enum | Champ metier de la table. |
| `Statut` | `StatutTicket` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateLimiteSla` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DatePremiereReponse` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateAffectation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateResolution` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `EstEscalade` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `NiveauEscalade` | `int` | Non | Metier | Champ metier de la table. |
| `DateDerniereEscalade` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `ResumeResolution` | `string?` | Oui | Metier | Champ metier de la table. |
| `NoteSatisfaction` | `int?` | Oui | Metier | Champ metier de la table. |
| `CommentaireSatisfaction` | `string?` | Oui | Metier | Champ metier de la table. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `ServiceCatalogueId` | `Guid?` | Oui | FK | Reference vers `SupportCatalogueServices`. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `AssigneAId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |
| `GroupeAssigneId` | `Guid?` | Oui | FK | Reference vers `GroupeAssigne`. |

### `MessagesTicket`

Role: Messages d'echange sur un ticket.
Source: `Data/Entities/MessageTicket.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `EstNoteInterne` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateEnvoi` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `TicketId` | `Guid` | Non | FK | Reference vers `Tickets`. |
| `AuteurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `TicketPiecesJointes`

Role: Pieces jointes attachees a un ticket.
Source: `Data/Entities/Ticket.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `TicketId` | `Guid` | Non | FK | Reference vers `Tickets`. |
| `NomOriginal` | `string` | Non | Metier | Champ metier de la table. |
| `UrlFichier` | `string` | Non | Metier | Champ metier de la table. |
| `TypeMime` | `string?` | Oui | Metier | Champ metier de la table. |
| `TailleOctets` | `long` | Non | Metier | Champ metier de la table. |
| `DateAjout` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `AjouteParId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `HistoriquesTicket`

Role: Historique des changements d'un ticket.
Source: `Data/Entities/HistoriqueTicket.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `TicketId` | `Guid` | Non | FK | Reference vers `Tickets`. |
| `AuteurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |
| `Commentaire` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `DateChangement` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `NotificationsUtilisateur`

Role: Notifications internes recues par les utilisateurs.
Source: `Data/Entities/NotificationUtilisateur.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `UserId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Message` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `Categorie` | `string` | Non | Metier | Champ metier de la table. |
| `Lien` | `string?` | Oui | Metier | Champ metier de la table. |
| `EstLue` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateLecture` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |

### `TransactionsFinancieres`

Role: Ecritures financieres liees aux scouts, activites, groupes ou projets.
Source: `Data/Entities/TransactionFinanciere.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Libelle` | `string` | Non | Metier | Champ metier de la table. |
| `Montant` | `decimal` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `Type` | `TypeTransaction` | Non | Enum | Champ metier de la table. |
| `Categorie` | `CategorieFinance` | Non | Enum | Champ metier de la table. |
| `DateTransaction` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `Reference` | `string?` | Oui | Metier | Libelle ou identifiant metier lisible. |
| `Commentaire` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `ActiviteId` | `Guid?` | Oui | FK | Reference vers `Activites`. |
| `ProjetAGRId` | `Guid?` | Oui | FK | Reference vers `ProjetsAGR`. |
| `ScoutId` | `Guid?` | Oui | FK | Reference vers `Scouts`. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `ProjetsAGR`

Role: Projets d'activites generatrices de revenus.
Source: `Data/Entities/ProjetAGR.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Statut` | `StatutProjetAGR` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `BudgetInitial` | `decimal` | Non | Metier | Champ metier de la table. |
| `DateDebut` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateFin` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `Responsable` | `string?` | Oui | Metier | Champ metier de la table. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `GroupeId` | `Guid?` | Oui | FK | Reference vers `Groupes`. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `CotisationsNationalesImports`

Role: Lots d'import des cotisations nationales.
Source: `Data/Entities/CotisationsNationales.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `AnneeReference` | `int` | Non | Metier | Champ metier de la table. |
| `NomFichier` | `string` | Non | Metier | Champ metier de la table. |
| `DateImport` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `MontantTotal` | `decimal` | Non | Metier | Champ metier de la table. |
| `NombreAjour` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `NombreNonAjour` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `NombreAVerifier` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `CotisationsNationalesImportLignes`

Role: Lignes detaillees d'un import de cotisations nationales.
Source: `Data/Entities/CotisationsNationales.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `ImportId` | `Guid` | Non | FK | Reference vers `CotisationsNationalesImports`. |
| `ScoutId` | `Guid?` | Oui | FK | Reference vers `Scouts`. |
| `Matricule` | `string` | Non | Metier | Champ metier de la table. |
| `NomImporte` | `string?` | Oui | Metier | Champ metier de la table. |
| `Montant` | `decimal?` | Oui | Metier | Valeur numerique de suivi ou de calcul. |
| `Statut` | `StatutLigneCotisationNationale` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `Motif` | `string?` | Oui | Metier | Champ metier de la table. |

Contraintes / notes:
- Index sur (ImportId, Matricule).

## LMS / formation

### `Formations`

Role: Cours ou parcours de formation du LMS.
Source: `Data/Entities/Formation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `ImageUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Niveau` | `NiveauFormation` | Non | Enum | Champ metier de la table. |
| `Statut` | `StatutFormation` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `DureeEstimeeHeures` | `int` | Non | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DatePublication` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DelivreBadge` | `bool` | Non | Metier | Champ metier de la table. |
| `DelivreAttestation` | `bool` | Non | Metier | Champ metier de la table. |
| `DelivreCertificat` | `bool` | Non | Metier | Champ metier de la table. |
| `DelivranceConfiguree` | `bool` | Non | Metier | Champ metier de la table. |
| `BrancheCibleId` | `Guid?` | Oui | FK | Reference vers `BrancheCible`. |
| `CompetenceLieeId` | `Guid?` | Oui | FK | Reference logique vers `Competences`. |
| `AuteurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `FormationsPrerequis`

Role: Relations de prerequis entre formations.
Source: `Data/Entities/FormationPrerequis.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |
| `PrerequisFormationId` | `Guid` | Non | FK | Reference vers `Formations`. |

### `ModulesFormation`

Role: Modules composant une formation.
Source: `Data/Entities/ModuleFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |

### `Lecons`

Role: Lecons appartenant a un module de formation.
Source: `Data/Entities/Lecon.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Type` | `TypeLecon` | Non | Enum | Champ metier de la table. |
| `ContenuTexte` | `string?` | Oui | Metier | Champ metier de la table. |
| `VideoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `DocumentUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |
| `DureeMinutes` | `int` | Non | Metier | Champ metier de la table. |
| `ModuleId` | `Guid` | Non | FK | Reference vers `ModulesFormation`. |

### `Quizzes`

Role: Quiz rattaches a un module de formation.
Source: `Data/Entities/Quiz.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `NoteMinimale` | `int` | Non | Metier | Champ metier de la table. |
| `NombreTentativesMax` | `int?` | Oui | Metier | Valeur numerique de suivi ou de calcul. |
| `DateOuvertureDisponibilite` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateFermetureDisponibilite` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `ModuleId` | `Guid` | Non | FK | Reference vers `ModulesFormation`. |

### `QuestionsQuiz`

Role: Questions d'un quiz.
Source: `Data/Entities/QuestionQuiz.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Enonce` | `string` | Non | Metier | Champ metier de la table. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |
| `QuizId` | `Guid` | Non | FK | Reference vers `Quizzes`. |

### `ReponsesQuiz`

Role: Reponses possibles a une question de quiz.
Source: `Data/Entities/ReponseQuiz.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Texte` | `string` | Non | Metier | Champ metier de la table. |
| `EstCorrecte` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |
| `QuestionId` | `Guid` | Non | FK | Reference vers `QuestionsQuiz`. |

### `SessionsFormation`

Role: Sessions publiees d'une formation.
Source: `Data/Entities/SessionFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `EstSelfPaced` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstPubliee` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateOuverture` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateFermeture` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |

### `InscriptionsFormation`

Role: Inscriptions des scouts aux formations.
Source: `Data/Entities/InscriptionFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `DateInscription` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `Statut` | `StatutInscription` | Non | Enum | Statut fonctionnel de l'enregistrement. |
| `DateTerminee` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `ProgressionPourcent` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |
| `SessionFormationId` | `Guid?` | Oui | FK | Reference vers `SessionsFormation`. |

Contraintes / notes:
- Unicite (ScoutId, FormationId).

### `ProgressionsLecon`

Role: Progression d'un scout sur une lecon.
Source: `Data/Entities/ProgressionLecon.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `EstTerminee` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateTerminee` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `LeconId` | `Guid` | Non | FK | Reference vers `Lecons`. |

Contraintes / notes:
- Unicite (ScoutId, LeconId).

### `TentativesQuiz`

Role: Tentatives d'un scout sur un quiz.
Source: `Data/Entities/TentativeQuiz.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Score` | `int` | Non | Metier | Valeur numerique de suivi ou de calcul. |
| `Reussi` | `bool` | Non | Metier | Champ metier de la table. |
| `DateTentative` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `ReponsesJson` | `string?` | Oui | Metier | Champ metier de la table. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `QuizId` | `Guid` | Non | FK | Reference vers `Quizzes`. |

### `CertificationsFormation`

Role: Certificats, attestations ou badges emis apres formation.
Source: `Data/Entities/CertificationFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Type` | `TypeCertificationFormation` | Non | Enum | Champ metier de la table. |
| `Code` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `DateEmission` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `ScoreFinal` | `int` | Non | Metier | Champ metier de la table. |
| `Mention` | `string` | Non | Metier | Champ metier de la table. |
| `ScoutId` | `Guid` | Non | FK | Reference vers `Scouts`. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |
| `InscriptionFormationId` | `Guid?` | Oui | FK | Reference vers `InscriptionsFormation`. |

Contraintes / notes:
- Code unique.
- Unicite (ScoutId, FormationId, Type).

### `JalonsFormation`

Role: Dates clefs et jalons d'une formation.
Source: `Data/Entities/JalonFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `DateJalon` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `Type` | `TypeJalonFormation` | Non | Enum | Champ metier de la table. |
| `EstPublie` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `AnnoncesFormation`

Role: Annonces publiees dans le contexte d'une formation.
Source: `Data/Entities/AnnonceFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `EstPubliee` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DatePublication` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |
| `AuteurId` | `Guid?` | Oui | FK | Reference vers `AspNetUsers`. |

### `DiscussionsFormation`

Role: Discussions ou fils de forum associes a une formation.
Source: `Data/Entities/DiscussionFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `ContenuInitial` | `string` | Non | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateDerniereActivite` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `EstVerrouillee` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `FormationId` | `Guid` | Non | FK | Reference vers `Formations`. |
| `AuteurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `MessagesDiscussionFormation`

Role: Messages postes dans une discussion de formation.
Source: `Data/Entities/MessageDiscussionFormation.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DiscussionFormationId` | `Guid` | Non | FK | Reference vers `DiscussionsFormation`. |
| `AuteurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

## Communication et vitrine publique

### `Actualites`

Role: Actualites publiees sur le portail.
Source: `Data/Entities/Actualite.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `ImageUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Resume` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `DatePublication` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `EstPublie` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `CreateurId` | `Guid` | Non | FK | Reference vers `AspNetUsers`. |

### `Galeries`

Role: Elements medias de la galerie.
Source: `Data/Entities/Galerie.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Titre` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `CheminMedia` | `string` | Non | Metier | Chemin, URL ou ressource associee. |
| `TypeMedia` | `string` | Non | Metier | Champ metier de la table. |
| `DateUpload` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `EstPublie` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |

### `MotsCommissaire`

Role: Mot du commissaire publie sur la plateforme.
Source: `Data/Entities/MotCommissaire.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Contenu` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `PhotoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Annee` | `int` | Non | Metier | Champ metier de la table. |
| `EstActif` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `LivreDor`

Role: Messages du livre d'or.
Source: `Data/Entities/LivreDor.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `NomAuteur` | `string` | Non | Metier | Champ metier de la table. |
| `Message` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `EstValide` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |
| `DateValidation` | `DateTime?` | Oui | Date | Date ou horodatage du processus metier. |

### `ContactMessages`

Role: Messages envoyes depuis le formulaire de contact.
Source: `Data/Entities/ContactMessage.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Email` | `string` | Non | Metier | Champ metier de la table. |
| `Sujet` | `string` | Non | Metier | Champ metier de la table. |
| `Message` | `string` | Non | Metier | Contenu textuel ou descriptif. |
| `Type` | `string` | Non | Metier | Champ metier de la table. |
| `EstLu` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateEnvoi` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `MembresHistoriques`

Role: Membres historiques ou anciens responsables mis en avant.
Source: `Data/Entities/MembreHistorique.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `PhotoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `Periode` | `string?` | Oui | Metier | Champ metier de la table. |
| `Categorie` | `CategorieHistorique` | Non | Enum | Champ metier de la table. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `Partenaires`

Role: Partenaires institutionnels ou prives.
Source: `Data/Entities/Partenaire.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Nom` | `string` | Non | Metier | Libelle ou identifiant metier lisible. |
| `Description` | `string?` | Oui | Metier | Contenu textuel ou descriptif. |
| `LogoUrl` | `string?` | Oui | Metier | Chemin, URL ou ressource associee. |
| `SiteWeb` | `string?` | Oui | Metier | Champ metier de la table. |
| `TypePartenariat` | `string?` | Oui | Metier | Champ metier de la table. |
| `EstActif` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `EstSupprime` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |
| `DateCreation` | `DateTime` | Non | Date | Date ou horodatage du processus metier. |

### `LiensReseauxSociaux`

Role: Liens de reseaux sociaux exposes sur la plateforme.
Source: `Data/Entities/Partenaire.cs`

| Champ | Type C# | Nullable | Role | Description |
|---|---|---|---|---|
| `Id` | `Guid` | Non | PK | Identifiant technique unique de l'enregistrement. |
| `Plateforme` | `string` | Non | Metier | Champ metier de la table. |
| `Url` | `string` | Non | Metier | Chemin, URL ou ressource associee. |
| `Icone` | `string?` | Oui | Metier | Champ metier de la table. |
| `EstActif` | `bool` | Non | Flag | Indicateur booleen de statut ou de visibilite. |
| `Ordre` | `int` | Non | Metier | Champ metier de la table. |

## Tables techniques Identity

### `AspNetRoles`

Role: Roles techniques ASP.NET Identity.

Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.

### `AspNetUserRoles`

Role: Jointure utilisateur-role Identity.

Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.

### `AspNetUserClaims`

Role: Claims utilises par Identity.

Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.

### `AspNetUserLogins`

Role: Logins externes Identity.

Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.

### `AspNetUserTokens`

Role: Tokens techniques Identity.

Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.

### `AspNetRoleClaims`

Role: Claims attaches aux roles Identity.

Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.

## Enumerations metier

- `StatutWorkflowDocument` : `Brouillon`, `Soumis`, `AReviser`, `Valide`
- `StatutInscriptionAnnuelle` : `Enregistree`, `Validee`, `Suspendue`
- `StatutLigneCotisationNationale` : `Ajour`, `NonAjour`, `AVerifier`

## Remarques

- Ce dictionnaire est centre sur le modele de donnees du depot et ses conventions EF Core.
- Les tables physiques peuvent contenir des colonnes supplementaires generees par EF Core ou PostgreSQL selon les migrations.
- `CompetenceLieeId` sur `Formations` est documente comme reference logique car aucune FK explicite n'est configuree dans `OnModelCreating`.
