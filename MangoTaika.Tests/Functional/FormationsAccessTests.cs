using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class FormationsAccessTests
{
    [Fact]
    public async Task Parent_CanViewAssignedChildFormations()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser parentUser = null!;
        Scout childScout = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Parent");
            parentUser = await TestDataSeeder.AddUserAsync(db, "Awa", "Parent", ["Parent"]);
            parentUser.PhoneNumber = "0700001111";

            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lms", []);
            childScout = CreateScout("7000401D", "Nadia", "Yao");
            var parent = CreateParent(parentUser.Id, childScout);
            var formation = CreateFormation(author.Id, "Formation Premiers Pas");

            db.Parents.Add(parent);
            db.Scouts.Add(childScout);
            db.Formations.Add(formation);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                ScoutId = childScout.Id
            });
        });

        using var client = factory.CreateAuthenticatedClient(parentUser.Id, "Parent");

        var response = await client.GetAsync($"/Formations/FormationsEnfant?scoutId={childScout.Id}");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Formation Premiers Pas");
        html.Should().Contain("Formations de");
    }

    [Fact]
    public async Task Parent_CannotViewOtherChildFormations()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser parentUser = null!;
        Scout otherScout = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Parent");
            parentUser = await TestDataSeeder.AddUserAsync(db, "Kadi", "Parent", ["Parent"]);
            parentUser.PhoneNumber = "0700002222";

            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lms", []);
            var linkedScout = CreateScout("7000402D", "Enfant", "Lie");
            otherScout = CreateScout("7000403D", "Autre", "Scout");
            var parent = CreateParent(parentUser.Id, linkedScout);
            var formation = CreateFormation(author.Id, "Formation Reservee");

            db.Parents.Add(parent);
            db.Scouts.AddRange(linkedScout, otherScout);
            db.Formations.Add(formation);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                ScoutId = otherScout.Id
            });
        });

        using var client = factory.CreateAuthenticatedClient(parentUser.Id, "Parent");

        var response = await client.GetAsync($"/Formations/FormationsEnfant?scoutId={otherScout.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Scout_CannotFollowFormationWithoutEnrollment()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Moussa", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lms", []);
            db.Scouts.Add(CreateScout("7000404D", "Moussa", "Scout", scoutUser.Id));
            formation = CreateFormation(author.Id, "Formation Non Inscrit");
            db.Formations.Add(formation);
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/Formations/Suivre?id={formation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Scout_CannotPassQuizWithoutEnrollment()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;
        Quiz quiz = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Aime", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lms", []);

            db.Scouts.Add(CreateScout("7000405D", "Aime", "Scout", scoutUser.Id));
            formation = CreateFormation(author.Id, "Formation Quiz");
            var module = new ModuleFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                Titre = "Module 1",
                Ordre = 1
            };
            quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Titre = "Quiz 1",
                NoteMinimale = 60
            };

            db.Formations.Add(formation);
            db.ModulesFormation.Add(module);
            db.Quizzes.Add(quiz);
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/Formations/PasserQuiz?quizId={quiz.Id}&formationId={formation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static Parent CreateParent(Guid userId, params Scout[] scouts)
    {
        return new Parent
        {
            Id = Guid.NewGuid(),
            Nom = "Parent",
            Prenom = "Test",
            UserId = userId,
            Scouts = scouts.ToList()
        };
    }

    private static Scout CreateScout(string matricule, string prenom, string nom, Guid? userId = null)
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
}
