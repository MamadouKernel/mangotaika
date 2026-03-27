using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class LmsParcoursPagesTests
{
    [Fact]
    public async Task MesFormations_CanFilterByCertifyingCourses()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Scout scout = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Ysee", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lms", []);

            scout = new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "7000501E",
                Prenom = "Ysee",
                Nom = "Scout",
                DateNaissance = new DateTime(2010, 4, 15),
                IsActive = true
            };

            var certif = CreateFormation(author.Id, "Parcours Certifiant", true);
            var libre = CreateFormation(author.Id, "Parcours Libre", false);

            db.Scouts.Add(scout);
            db.Formations.AddRange(certif, libre);
            db.InscriptionsFormation.AddRange(
                new InscriptionFormation
                {
                    Id = Guid.NewGuid(),
                    ScoutId = scout.Id,
                    FormationId = certif.Id
                },
                new InscriptionFormation
                {
                    Id = Guid.NewGuid(),
                    ScoutId = scout.Id,
                    FormationId = libre.Id
                });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Formations/MesFormations?certifiant=true");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Parcours Certifiant");
        html.Should().NotContain("Parcours Libre");
    }

    [Fact]
    public async Task PasserQuiz_ShowsAttemptHistory()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;
        Quiz quiz = null!;
        Scout scout = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Adja", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Quiz", []);

            scout = new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "7000502E",
                Prenom = "Adja",
                Nom = "Scout",
                DateNaissance = new DateTime(2010, 5, 10),
                IsActive = true
            };
            formation = CreateFormation(author.Id, "Parcours Quiz", true);
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
                Titre = "Quiz final",
                NoteMinimale = 70
            };
            var question = new QuestionQuiz
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                Enonce = "Question",
                Ordre = 1
            };
            question.Reponses.Add(new ReponseQuiz
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Texte = "Bonne",
                EstCorrecte = true,
                Ordre = 1
            });

            db.Scouts.Add(scout);
            db.Formations.Add(formation);
            db.ModulesFormation.Add(module);
            db.Quizzes.Add(quiz);
            db.QuestionsQuiz.Add(question);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                ScoutId = scout.Id,
                FormationId = formation.Id
            });
            db.TentativesQuiz.Add(new TentativeQuiz
            {
                Id = Guid.NewGuid(),
                ScoutId = scout.Id,
                QuizId = quiz.Id,
                Score = 82,
                Reussi = true,
                DateTentative = DateTime.UtcNow.AddDays(-1)
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/Formations/PasserQuiz?quizId={quiz.Id}&formationId={formation.Id}");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Historique des tentatives");
        html.Should().Contain("82%");
        html.Should().Contain("Evaluation validee");
    }

    [Fact]
    public async Task PasserQuiz_ShowsLockedState_WhenLessonsAreNotCompleted()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Formation formation = null!;
        Quiz quiz = null!;
        Scout scout = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Yao", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Lock", []);

            scout = new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "7000503E",
                Prenom = "Yao",
                Nom = "Scout",
                DateNaissance = new DateTime(2010, 8, 10),
                IsActive = true
            };
            formation = CreateFormation(author.Id, "Parcours verrouille", true);
            var module = new ModuleFormation
            {
                Id = Guid.NewGuid(),
                FormationId = formation.Id,
                Titre = "Module verrouille",
                Ordre = 1
            };
            var lecon = new Lecon
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Titre = "Lecon preparatoire",
                Type = TypeLecon.Texte,
                ContenuTexte = "Contenu",
                DureeMinutes = 15,
                Ordre = 1
            };
            quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Titre = "Quiz bloque",
                NoteMinimale = 70
            };
            var question = new QuestionQuiz
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                Enonce = "Question",
                Ordre = 1
            };
            question.Reponses.Add(new ReponseQuiz
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Texte = "Bonne",
                EstCorrecte = true,
                Ordre = 1
            });

            db.Scouts.Add(scout);
            db.Formations.Add(formation);
            db.ModulesFormation.Add(module);
            db.Lecons.Add(lecon);
            db.Quizzes.Add(quiz);
            db.QuestionsQuiz.Add(question);
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                ScoutId = scout.Id,
                FormationId = formation.Id
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/Formations/PasserQuiz?quizId={quiz.Id}&formationId={formation.Id}");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Evaluation en attente");
        html.Should().Contain("Terminez toutes les lecons du module");
    }

    private static Formation CreateFormation(Guid authorId, string title, bool certifying)
    {
        return new Formation
        {
            Id = Guid.NewGuid(),
            AuteurId = authorId,
            Titre = title,
            Description = "Description",
            Statut = StatutFormation.Publiee,
            DatePublication = DateTime.UtcNow.AddDays(-1),
            DelivreBadge = certifying,
            DelivreAttestation = certifying,
            DelivreCertificat = false
        };
    }
}
