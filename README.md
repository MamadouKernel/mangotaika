# MangoTaika

Plateforme web ASP.NET Core MVC pour le District Scout MANGO TAIKA.

Le projet couvre plusieurs domaines dans une seule application:
- gestion des utilisateurs et des rôles
- gestion des scouts, groupes, branches et activités
- demandes administratives et demandes de groupe
- centre de support inspiré ServiceNow
- LMS interne avec parcours, sessions, quiz, certificats et forum de formation
- finances, AGR, actualités, galerie, partenaires et historique

## Stack technique

- `.NET 9`
- `ASP.NET Core MVC`
- `Entity Framework Core`
- `PostgreSQL`
- `ASP.NET Core Identity`
- `SignalR`
- `ClosedXML` pour les exports/imports Excel
- `Docker` / `docker compose` pour le déploiement

## Modules principaux

- `Support`
  - tickets, SLA, escalades, assignation intelligente, notifications, base de connaissances, catalogue de services
- `LMS`
  - catalogue, inscriptions, sessions, jalons, annonces, quiz, certificats, forum de classe
- `Administration`
  - rôles, utilisateurs, dashboard, paramètres métier
- `Communication`
  - actualités, galerie, mot du commissaire, réseaux sociaux

## Rôles

Les rôles gérés par l’application:
- `Administrateur`
- `Gestionnaire`
- `AgentSupport`
- `Scout`
- `Parent`
- `Superviseur`
- `Consultant`

## Démarrage local

### Prérequis

- `.NET SDK 9`
- `PostgreSQL`

### Configuration

Mettre à jour [appsettings.json](C:/Users/kerne/Downloads/rodi/new/MangoTaika/appsettings.json) ou utiliser des variables d’environnement:

- `ConnectionStrings__DefaultConnection`
- `AdminSeed__Email`
- `AdminSeed__Phone`
- `AdminSeed__Password`
- `Contact__Email`
- `Contact__WhatsAppNumber`
- `SeedDemoData`
- `DataProtection__KeysPath`

Important:
- l’admin seed n’est créé que si `AdminSeed__Password` est renseigné
- en production, il est recommandé de laisser `SeedDemoData=false`

### Commandes

```powershell
dotnet restore
dotnet ef database update
dotnet run
```

Application:
- URL locale typique: `https://localhost:xxxx` ou `http://localhost:xxxx`
- health check: `/health`

## Tests

Projet de tests: [MangoTaika.Tests](C:/Users/kerne/Downloads/rodi/new/MangoTaika/MangoTaika.Tests/MangoTaika.Tests.csproj)

Lancer les tests:

```powershell
dotnet test .\MangoTaika.Tests\MangoTaika.Tests.csproj
```

Couverture actuelle:
- tests unitaires
- tests d’intégration
- tests fonctionnels HTTP

## Docker

Fichiers fournis:
- [Dockerfile](C:/Users/kerne/Downloads/rodi/new/MangoTaika/Dockerfile)
- [docker-compose.yml](C:/Users/kerne/Downloads/rodi/new/MangoTaika/docker-compose.yml)
- [.env.example](C:/Users/kerne/Downloads/rodi/new/MangoTaika/.env.example)

### Déploiement rapide avec Docker Compose

1. Copier `.env.example` en `.env`
2. Remplir les valeurs sensibles
3. Lancer:

```bash
docker compose up -d --build
```

Services lancés:
- `db` : PostgreSQL
- `app` : application MangoTaika

Volumes persistants:
- base PostgreSQL
- uploads
- clés Data Protection

## Déploiement VPS

### Recommandation

Déployer derrière un reverse proxy `Nginx` avec HTTPS.

### Étapes minimales

1. Installer Docker et Docker Compose
2. Cloner le repository
3. Créer le fichier `.env`
4. Renseigner au minimum:
   - `POSTGRES_PASSWORD`
   - `ADMIN_SEED_PASSWORD`
   - `ADMIN_SEED_EMAIL`
   - `ADMIN_SEED_PHONE`
5. Lancer:

```bash
docker compose up -d --build
```

### Vérifications

```bash
docker compose ps
docker compose logs -f app
curl http://localhost:8080/health
```

## Variables d’environnement utiles

Exemples disponibles dans [.env.example](C:/Users/kerne/Downloads/rodi/new/MangoTaika/.env.example):

- `APP_PORT`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `ADMIN_SEED_EMAIL`
- `ADMIN_SEED_PHONE`
- `ADMIN_SEED_PASSWORD`
- `CONTACT_EMAIL`
- `CONTACT_WHATSAPP_NUMBER`
- `SEED_DEMO_DATA`
- `TZ`

## Sécurité et publication

Quelques points déjà en place:
- migrations automatiques au démarrage
- rôle admin seed optionnel
- `ForwardedHeaders` pour reverse proxy
- persistance des clés `DataProtection`
- protection antiforgery
- cookies et en-têtes de sécurité
- endpoint `/health`

## Repository

Repository GitHub:
[https://github.com/MamadouKernel/mangotaika.git](https://github.com/MamadouKernel/mangotaika.git)

Branche principale:
- `main`

## Note

Pour un déploiement public, ne pas conserver de mots de passe réels dans `appsettings.json`. Utiliser uniquement des variables d’environnement ou le fichier `.env` non versionné.
