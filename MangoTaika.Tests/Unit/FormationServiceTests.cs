using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class FormationServiceTests
{
    [Fact]
    public async Task GetProgressionAsync_ReturnsNull_WhenScoutIsNotEnrolled()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: false);
        var service = new FormationService(db);

        var progression = await service.GetProgressionAsync(data.Formation.Id, data.Scout.Id);

        progression.Should().BeNull();
    }

    [Fact]
    public async Task MarquerLeconTermineeAsync_Throws_WhenScoutIsNotEnrolled()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: false);
        var service = new FormationService(db);

        Func<Task> act = () => service.MarquerLeconTermineeAsync(data.Lecon.Id, data.Scout.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pas inscrit*");
    }

    [Fact]
    public async Task SoumettreQuizAsync_Throws_WhenScoutIsNotEnrolled()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: false);
        var service = new FormationService(db);

        Func<Task> act = () => service.SoumettreQuizAsync(
            data.Quiz!.Id,
            data.Scout.Id,
            new Dictionary<Guid, Guid> { [data.Question!.Id] = data.CorrectAnswer!.Id });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pas inscrit*");
    }

    [Fact]
    public async Task MarquerLeconTermineeAsync_CreatesProgression_WhenScoutIsEnrolled()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true);
        var service = new FormationService(db);

        await service.MarquerLeconTermineeAsync(data.Lecon.Id, data.Scout.Id);

        var progression = await db.ProgressionsLecon.SingleAsync(p => p.LeconId == data.Lecon.Id && p.ScoutId == data.Scout.Id);
        progression.EstTerminee.Should().BeTrue();

        var inscription = await db.InscriptionsFormation.SingleAsync(i => i.FormationId == data.Formation.Id && i.ScoutId == data.Scout.Id);
        inscription.ProgressionPourcent.Should().Be(100);

    }

    [Fact]
    public async Task InscrireScoutAsync_AssignsSelfPacedSession_WhenAvailable()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: false);
        var service = new FormationService(db);

        var scheduled = new SessionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Session datee",
            EstPubliee = true,
            DateOuverture = DateTime.UtcNow.AddDays(2),
            DateFermeture = DateTime.UtcNow.AddDays(20)
        };
        var selfPaced = new SessionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Parcours libre",
            EstSelfPaced = true,
            EstPubliee = true
        };

        db.SessionsFormation.AddRange(scheduled, selfPaced);
        await db.SaveChangesAsync();

        var inscription = await service.InscrireScoutAsync(data.Formation.Id, data.Scout.Id);

        inscription.SessionFormationId.Should().Be(selfPaced.Id);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsPublishedSessionsAndAnnouncements()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: false);
        var service = new FormationService(db);
        var author = await db.Users.FirstAsync(u => u.Id == data.Formation.AuteurId);

        db.SessionsFormation.Add(new SessionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Session Avril",
            EstPubliee = true,
            DateOuverture = DateTime.UtcNow.AddDays(-1),
            DateFermeture = DateTime.UtcNow.AddDays(10)
        });
        db.AnnoncesFormation.Add(new AnnonceFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Bienvenue",
            Contenu = "Ouverture de la session.",
            EstPubliee = true,
            AuteurId = author.Id
        });
        await db.SaveChangesAsync();

        var detail = await service.GetDetailAsync(data.Formation.Id);

        detail.Should().NotBeNull();
        detail!.Sessions.Should().ContainSingle(s => s.Titre == "Session Avril");
        detail.Annonces.Should().ContainSingle(a => a.Titre == "Bienvenue");
        detail.SessionTitre.Should().Be("Session Avril");
    }

    [Fact]
    public async Task MarquerLeconTermineeAsync_CreatesCertifications_WhenFormationIsCompleted()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true, includeQuiz: false);
        var service = new FormationService(db);

        await service.MarquerLeconTermineeAsync(data.Lecon.Id, data.Scout.Id);

        var certifications = await db.CertificationsFormation
            .Where(c => c.FormationId == data.Formation.Id && c.ScoutId == data.Scout.Id)
            .ToListAsync();

        certifications.Should().HaveCount(2);
        certifications.Select(c => c.Type).Should().Contain([TypeCertificationFormation.Badge, TypeCertificationFormation.Attestation]);
    }

    [Fact]
    public async Task MarquerLeconTermineeAsync_DoesNotDuplicateCertifications_WhenFormationAlreadyCompleted()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true, includeQuiz: false);
        var service = new FormationService(db);

        await service.MarquerLeconTermineeAsync(data.Lecon.Id, data.Scout.Id);
        await service.MarquerLeconTermineeAsync(data.Lecon.Id, data.Scout.Id);

        var certifications = await db.CertificationsFormation
            .Where(c => c.FormationId == data.Formation.Id && c.ScoutId == data.Scout.Id)
            .ToListAsync();

        certifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetParcoursScoutsAsync_ReturnsHighLevelMoocIndicators()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true);
        var service = new FormationService(db);

        db.SessionsFormation.Add(new SessionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Cohorte Avril",
            EstPubliee = true,
            DateOuverture = DateTime.UtcNow.AddDays(3),
            DateFermeture = DateTime.UtcNow.AddDays(20)
        });
        db.AnnoncesFormation.Add(new AnnonceFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Bienvenue",
            Contenu = "Lancement a venir",
            EstPubliee = true
        });
        db.DiscussionsFormation.Add(new DiscussionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            AuteurId = data.Formation.AuteurId,
            Titre = "Preparation",
            ContenuInitial = "Quels sont les prerequis ?",
            DateDerniereActivite = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var parcours = await service.GetParcoursScoutsAsync([data.Scout.Id]);

        parcours.Should().ContainSingle();
        var item = parcours[0];
        item.Titre.Should().Be(data.Formation.Titre);
        item.NombreModules.Should().Be(1);
        item.NombreQuiz.Should().Be(1);
        item.NombreAnnonces.Should().Be(1);
        item.NombreDiscussions.Should().Be(1);
        item.EtatPedagogique.Should().Be("Session a venir");
        item.EtatEvaluation.Should().Be("Quiz a demarrer");
        item.EtatCertifiant.Should().Be("Parcours certifiant en cours");
        item.ProchaineEtape.Should().StartWith("Se preparer pour l'ouverture");
    }

    [Fact]
    public async Task GetQuizPassageAsync_ReturnsAttemptHistoryOrdered()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true);
        var service = new FormationService(db);

        db.TentativesQuiz.AddRange(
            new TentativeQuiz
            {
                Id = Guid.NewGuid(),
                ScoutId = data.Scout.Id,
                QuizId = data.Quiz!.Id,
                Score = 45,
                Reussi = false,
                DateTentative = DateTime.UtcNow.AddDays(-2)
            },
            new TentativeQuiz
            {
                Id = Guid.NewGuid(),
                ScoutId = data.Scout.Id,
                QuizId = data.Quiz!.Id,
                Score = 80,
                Reussi = true,
                DateTentative = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var page = await service.GetQuizPassageAsync(data.Quiz.Id, data.Formation.Id, data.Scout.Id);

        page.Should().NotBeNull();
        page!.NombreTentatives.Should().Be(2);
        page.MeilleurScore.Should().Be(80);
        page.EtatEvaluation.Should().Be("Evaluation validee");
        page.Tentatives.Should().HaveCount(2);
        page.Tentatives[0].Score.Should().Be(80);
        page.Tentatives[1].Score.Should().Be(45);
    }

    [Fact]
    public async Task GetProgressionAsync_ReturnsReadOnly_WhenSessionIsUpcoming()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true);
        var service = new FormationService(db);

        var session = new SessionFormation
        {
            Id = Guid.NewGuid(),
            FormationId = data.Formation.Id,
            Titre = "Cohorte Juin",
            EstPubliee = true,
            DateOuverture = DateTime.UtcNow.AddDays(5),
            DateFermeture = DateTime.UtcNow.AddDays(20)
        };
        db.SessionsFormation.Add(session);

        var inscription = await db.InscriptionsFormation.SingleAsync(i => i.FormationId == data.Formation.Id && i.ScoutId == data.Scout.Id);
        inscription.SessionFormationId = session.Id;
        await db.SaveChangesAsync();

        var progression = await service.GetProgressionAsync(data.Formation.Id, data.Scout.Id);

        progression.Should().NotBeNull();
        progression!.PeutInteragir.Should().BeFalse();
        progression.EstLectureSeule.Should().BeTrue();
        progression.MessageAcces.Should().Contain("ouvrira");
        progression.Modules.Should().OnlyContain(m => !m.EstDisponible);
    }

    [Fact]
    public async Task MarquerLeconTermineeAsync_Throws_WhenPreviousLessonIsNotCompleted()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true, includeQuiz: false);
        var service = new FormationService(db);

        var secondLesson = new Lecon
        {
            Id = Guid.NewGuid(),
            ModuleId = data.Module.Id,
            Titre = "Lecon 2",
            Type = TypeLecon.Texte,
            ContenuTexte = "Suite",
            DureeMinutes = 8,
            Ordre = 2
        };
        db.Lecons.Add(secondLesson);
        await db.SaveChangesAsync();

        Func<Task> act = () => service.MarquerLeconTermineeAsync(secondLesson.Id, data.Scout.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*debloquer cette lecon*");
    }

    [Fact]
    public async Task SoumettreQuizAsync_Throws_WhenModuleLessonsAreNotCompleted()
    {
        await using var db = CreateDbContext();
        var data = await SeedFormationGraphAsync(db, enrolled: true);
        var service = new FormationService(db);

        Func<Task> act = () => service.SoumettreQuizAsync(
            data.Quiz!.Id,
            data.Scout.Id,
            new Dictionary<Guid, Guid> { [data.Question!.Id] = data.CorrectAnswer!.Id });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Terminez toutes les lecons du module*");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<FormationGraphData> SeedFormationGraphAsync(AppDbContext db, bool enrolled, bool includeQuiz = true)
    {
        await TestDataSeeder.EnsureRolesAsync(db, "Scout");
        var author = await TestDataSeeder.AddUserAsync(db, "Auteur", "Lms", []);
        var scoutUser = await TestDataSeeder.AddUserAsync(db, "Scout", "Lms", ["Scout"]);

        var scout = new Scout
        {
            Id = Guid.NewGuid(),
            UserId = scoutUser.Id,
            Matricule = "7000701G",
            Prenom = "Scout",
            Nom = "Lms",
            DateNaissance = new DateTime(2010, 1, 10),
            IsActive = true
        };

        var formation = new Formation
        {
            Id = Guid.NewGuid(),
            AuteurId = author.Id,
            Titre = "Formation test",
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

        Quiz? quiz = null;
        QuestionQuiz? question = null;
        ReponseQuiz? correctAnswer = null;

        if (includeQuiz)
        {
            quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Titre = "Quiz 1",
                NoteMinimale = 60
            };

            question = new QuestionQuiz
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                Enonce = "Question 1",
                Ordre = 1
            };

            correctAnswer = new ReponseQuiz
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Texte = "Bonne",
                EstCorrecte = true,
                Ordre = 1
            };

            var wrongAnswer = new ReponseQuiz
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Texte = "Mauvaise",
                EstCorrecte = false,
                Ordre = 2
            };

            question.Reponses = [correctAnswer, wrongAnswer];
        }

        db.Scouts.Add(scout);
        db.Formations.Add(formation);
        db.ModulesFormation.Add(module);
        db.Lecons.Add(lecon);
        if (quiz != null)
            db.Quizzes.Add(quiz);
        if (question != null)
            db.QuestionsQuiz.Add(question);

        if (enrolled)
        {
            db.InscriptionsFormation.Add(new InscriptionFormation
            {
                Id = Guid.NewGuid(),
                ScoutId = scout.Id,
                FormationId = formation.Id
            });
        }

        await db.SaveChangesAsync();

        return new FormationGraphData(formation, module, lecon, quiz, question, correctAnswer, scout);
    }

    private sealed record FormationGraphData(
        Formation Formation,
        ModuleFormation Module,
        Lecon Lecon,
        Quiz? Quiz,
        QuestionQuiz? Question,
        ReponseQuiz? CorrectAnswer,
        Scout Scout);
}
