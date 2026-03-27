using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class ForumFormationsPagesTests
{
    [Fact]
    public async Task Scout_CanViewForum_WhenEnrolled()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Aimee", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Forum", []);

            formation = CreateFormation(author.Id, "Forum Orientation");
            var scout = CreateScout("7000301C", "Aimee", "Scout", scoutUser.Id);
            var discussion = CreateDiscussion(formation.Id, author.Id, "Bienvenue sur le forum");

            db.Formations.Add(formation);
            db.Scouts.Add(scout);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                ScoutId = scout.Id
            });
            db.DiscussionsFormation.Add(discussion);
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/ForumFormations/Index?formationId={formation.Id}");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Bienvenue sur le forum");
        html.Should().Contain("Publier la discussion");
    }

    [Fact]
    public async Task Scout_CannotViewForum_WhenNotEnrolled()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Mariam", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Forum", []);

            formation = CreateFormation(author.Id, "Forum Reserve");
            db.Formations.Add(formation);
            db.Scouts.Add(CreateScout("7000302C", "Mariam", "Scout", scoutUser.Id));
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/ForumFormations/Index?formationId={formation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Scout_CanCreateDiscussion_WhenEnrolled()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Yao", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Forum", []);

            formation = CreateFormation(author.Id, "Forum Creation");
            var scout = CreateScout("7000303C", "Yao", "Scout", scoutUser.Id);

            db.Formations.Add(formation);
            db.Scouts.Add(scout);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                ScoutId = scout.Id
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");
        var forumHtml = await client.GetStringAsync($"/ForumFormations/Index?formationId={formation.Id}");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(forumHtml);

        var request = new HttpRequestMessage(HttpMethod.Post, "/ForumFormations/NouvelleDiscussion");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["formationId"] = formation.Id.ToString(),
            ["Titre"] = "Question sur le module 1",
            ["Contenu"] = "Pouvez-vous preciser le travail attendu ?"
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/ForumFormations/Discussion");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MangoTaika.Data.AppDbContext>();
        db.DiscussionsFormation.Should().ContainSingle(d =>
            d.FormationId == formation.Id &&
            d.Titre == "Question sur le module 1");
    }

    [Fact]
    public async Task Consultant_CanReadDiscussion_ButCannotPost()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser consultantUser = null!;
        DiscussionFormation discussion = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Consultant");
            consultantUser = await TestDataSeeder.AddUserAsync(db, "Rita", "Consultant", ["Consultant"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Forum", []);
            var formation = CreateFormation(author.Id, "Forum Lecture Seule");
            discussion = CreateDiscussion(formation.Id, author.Id, "Questions frequentes");

            db.Formations.Add(formation);
            db.DiscussionsFormation.Add(discussion);
        });

        using var client = factory.CreateAuthenticatedClient(consultantUser.Id, "Consultant");
        var discussionResponse = await client.GetAsync($"/ForumFormations/Discussion/{discussion.Id}");
        var html = await discussionResponse.Content.ReadAsStringAsync();
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(html);

        discussionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Lecture seule active");
        html.Should().NotContain("Ajouter une reponse");

        var request = new HttpRequestMessage(HttpMethod.Post, "/ForumFormations/AjouterMessage");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["discussionId"] = discussion.Id.ToString(),
            ["Contenu"] = "Je tente quand meme une reponse."
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static Formation CreateFormation(Guid authorId, string title)
    {
        return new Formation
        {
            Id = Guid.NewGuid(),
            AuteurId = authorId,
            Titre = title,
            Description = "Description",
            Statut = StatutFormation.Publiee,
            DatePublication = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static Scout CreateScout(string matricule, string prenom, string nom, Guid userId)
    {
        return new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = matricule,
            Prenom = prenom,
            Nom = nom,
            UserId = userId,
            DateNaissance = new DateTime(2010, 1, 15),
            IsActive = true
        };
    }

    private static DiscussionFormation CreateDiscussion(Guid formationId, Guid authorId, string title)
    {
        return new DiscussionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = formationId,
            AuteurId = authorId,
            Titre = title,
            ContenuInitial = "Contenu d'ouverture",
            DateCreation = DateTime.UtcNow.AddHours(-2),
            DateDerniereActivite = DateTime.UtcNow.AddHours(-1)
        };
    }
}
