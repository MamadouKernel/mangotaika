using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class SupportPagesTests
{
    [Fact]
    public async Task MesTickets_PageLoadsForScout()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Aya", "Scout", ["Scout"]);
            db.Tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                NumeroTicket = "INC-SCOUT-1",
                Sujet = "Ticket scout",
                Description = "Description scout",
                CreateurId = scoutUser.Id,
                Statut = StatutTicket.Nouveau,
                Type = TypeTicket.Requete,
                Categorie = CategorieTicket.Administrative,
                Impact = ImpactTicket.Faible,
                Urgence = UrgenceTicket.Faible,
                Priorite = PrioriteTicket.Basse,
                DateCreation = DateTime.UtcNow,
                DateLimiteSla = DateTime.UtcNow.AddHours(24)
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Tickets/MesTickets");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Mes tickets");
        html.Should().Contain("Ticket scout");
    }

    [Fact]
    public async Task Dashboard_PageLoadsSupportMetricsForAgent()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser agentUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");
            agentUser = await TestDataSeeder.AddUserAsync(db, "Nina", "Support", ["AgentSupport"]);
            var creator = await TestDataSeeder.AddUserAsync(db, "Ibra", "Demandeur", []);

            db.NotificationsUtilisateur.Add(new NotificationUtilisateur
            {
                Id = Guid.NewGuid(),
                UserId = agentUser.Id,
                Titre = "Escalade",
                Message = "Notification support",
                Categorie = "Support"
            });

            db.Tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                NumeroTicket = "INC-AGENT-1",
                Sujet = "Ticket agent",
                Description = "Description agent",
                CreateurId = creator.Id,
                AssigneAId = agentUser.Id,
                Statut = StatutTicket.Affecte,
                Type = TypeTicket.Incident,
                Categorie = CategorieTicket.Technique,
                Impact = ImpactTicket.Moyen,
                Urgence = UrgenceTicket.Haute,
                Priorite = PrioriteTicket.Haute,
                DateCreation = DateTime.UtcNow.AddHours(-2),
                DateLimiteSla = DateTime.UtcNow.AddHours(2)
            });
        });

        using var client = factory.CreateAuthenticatedClient(agentUser.Id, "AgentSupport");

        var response = await client.GetAsync("/Dashboard");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Mes tickets assignes");
        html.Should().Contain("Notifications non lues");
        html.Should().Contain("Respect SLA");
    }

    [Fact]
    public async Task Dashboard_PageLoadsMoocMetricsForScout()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Scout scout = null!;
        Formation formation = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Lina", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lms", []);

            scout = new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "SC-DASH-001",
                Prenom = "Lina",
                Nom = "Scout",
                DateNaissance = new DateTime(2010, 2, 10),
                IsActive = true
            };
            formation = new Formation
            {
                Id = Guid.NewGuid(),
                AuteurId = author.Id,
                Titre = "Parcours Orientation",
                Description = "Description",
                Statut = StatutFormation.Publiee,
                DatePublication = DateTime.UtcNow.AddDays(-1)
            };
            var module = new ModuleFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                Titre = "Module 1",
                Ordre = 1
            };
            var lecon = new Lecon
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Titre = "Lecon 1",
                Type = TypeLecon.Texte,
                ContenuTexte = "Contenu",
                DureeMinutes = 10,
                Ordre = 1
            };

            db.Scouts.Add(scout);
            db.Formations.Add(formation);
            db.ModulesFormation.Add(module);
            db.Lecons.Add(lecon);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                ScoutId = scout.Id,
                FormationId = formation.Id,
                ProgressionPourcent = 25
            });
            db.AnnoncesFormation.Add(new AnnonceFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                Titre = "Bienvenue",
                Contenu = "Annonce test",
                EstPubliee = true
            });
            db.DiscussionsFormation.Add(new DiscussionFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                AuteurId = author.Id,
                Titre = "Forum",
                ContenuInitial = "Echange",
                DateDerniereActivite = DateTime.UtcNow
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Dashboard");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Progression LMS moyenne");
        html.Should().Contain("Discussions actives");
        html.Should().Contain("Mes parcours LMS");
        html.Should().Contain("Parcours Orientation");
    }
}
