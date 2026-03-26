using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class SupportKnowledgePagesTests
{
    [Fact]
    public async Task SupportCatalog_Index_HidesManagementActions_ForConsultant()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser consultant = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Consultant");
            consultant = await TestDataSeeder.AddUserAsync(db, "Madi", "Consultant", ["Consultant"]);
            db.SupportCatalogueServices.Add(new SupportServiceCatalogueItem
            {
                Id = Guid.NewGuid(),
                Code = "SRV-MAIL",
                Nom = "Messagerie",
                Description = "Support sur la messagerie",
                EstActif = true,
                DelaiSlaHeures = 6
            });
        });

        using var client = factory.CreateAuthenticatedClient(consultant.Id, "Consultant");

        var response = await client.GetAsync("/SupportCatalog");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Messagerie");
        html.Should().NotContain("Nouveau service");
        html.Should().NotContain("Desactiver");
    }

    [Fact]
    public async Task SupportCatalog_Create_LoadsForAgentSupport()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser agent = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");
            agent = await TestDataSeeder.AddUserAsync(db, "Nina", "Agent", ["AgentSupport"]);
        });

        using var client = factory.CreateAuthenticatedClient(agent.Id, "AgentSupport");

        var response = await client.GetAsync("/SupportCatalog/Create");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Nouveau service catalogue");
    }

    [Fact]
    public async Task KnowledgeBase_Index_ShowsOnlyPublishedArticles_ForConsultant()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser consultant = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Consultant");
            consultant = await TestDataSeeder.AddUserAsync(db, "Rita", "Consultant", ["Consultant"]);
            db.SupportKnowledgeArticles.AddRange(
                new SupportKnowledgeArticle
                {
                    Id = Guid.NewGuid(),
                    Titre = "Procedure VPN",
                    Resume = "Resume public",
                    Contenu = "Contenu public",
                    Categorie = "Reseau",
                    EstPublie = true
                },
                new SupportKnowledgeArticle
                {
                    Id = Guid.NewGuid(),
                    Titre = "Brouillon interne",
                    Resume = "Resume prive",
                    Contenu = "Contenu prive",
                    Categorie = "Interne",
                    EstPublie = false
                });
        });

        using var client = factory.CreateAuthenticatedClient(consultant.Id, "Consultant");

        var response = await client.GetAsync("/KnowledgeBase");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Procedure VPN");
        html.Should().NotContain("Brouillon interne");
        html.Should().NotContain("Nouvel article");
    }

    [Fact]
    public async Task KnowledgeBase_Details_ReturnsNotFound_ForDraftWhenConsultant()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser consultant = null!;
        SupportKnowledgeArticle draft = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Consultant");
            consultant = await TestDataSeeder.AddUserAsync(db, "Sarah", "Consultant", ["Consultant"]);
            draft = new SupportKnowledgeArticle
            {
                Id = Guid.NewGuid(),
                Titre = "Runbook prive",
                Resume = "Interne",
                Contenu = "Interne",
                Categorie = "Operations",
                EstPublie = false
            };
            db.SupportKnowledgeArticles.Add(draft);
        });

        using var client = factory.CreateAuthenticatedClient(consultant.Id, "Consultant");

        var response = await client.GetAsync($"/KnowledgeBase/Details/{draft.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task KnowledgeBase_Index_ShowsDrafts_ForAgentSupport()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser agent = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");
            agent = await TestDataSeeder.AddUserAsync(db, "Paul", "Agent", ["AgentSupport"]);
            db.SupportKnowledgeArticles.AddRange(
                new SupportKnowledgeArticle
                {
                    Id = Guid.NewGuid(),
                    Titre = "Guide public",
                    Resume = "Visible",
                    Contenu = "Visible",
                    Categorie = "FAQ",
                    EstPublie = true
                },
                new SupportKnowledgeArticle
                {
                    Id = Guid.NewGuid(),
                    Titre = "Guide brouillon",
                    Resume = "En cours",
                    Contenu = "En cours",
                    Categorie = "FAQ",
                    EstPublie = false
                });
        });

        using var client = factory.CreateAuthenticatedClient(agent.Id, "AgentSupport");

        var response = await client.GetAsync("/KnowledgeBase");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Guide public");
        html.Should().Contain("Guide brouillon");
        html.Should().Contain("Nouvel article");
    }
}
