using System.Net;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class BranchesPagesTests
{
    [Fact]
    public async Task Create_Rejects_ChefUnite_From_Another_Groupe()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupeA = null!;
        Groupe groupeB = null!;
        Scout chefAutreGroupe = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupeA = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe A" };
            groupeB = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe B" };
            db.Groupes.AddRange(groupeA, groupeB);

            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583753X",
                Nom = "Chef",
                Prenom = "Alpha",
                DateNaissance = new DateTime(2010, 1, 1),
                GroupeId = groupeA.Id
            });

            chefAutreGroupe = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583754X",
                Nom = "Chef",
                Prenom = "Beta",
                DateNaissance = new DateTime(2010, 1, 1),
                GroupeId = groupeB.Id
            };
            db.Scouts.Add(chefAutreGroupe);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var createHtml = await client.GetStringAsync("/Branches/Create");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(createHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Branches/Create");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nom"] = "Louveteaux",
            ["Description"] = "Branche test",
            ["AgeMin"] = "8",
            ["AgeMax"] = "12",
            ["GroupeId"] = groupeA.Id.ToString(),
            ["ChefUniteId"] = chefAutreGroupe.Id.ToString()
        });

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("doit appartenir au groupe");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Branches.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_Rejects_Duplicate_Name_In_Same_Groupe()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupe = null!;
        Scout chef = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe A" };
            db.Groupes.Add(groupe);

            chef = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583755X",
                Nom = "Chef",
                Prenom = "Alpha",
                DateNaissance = new DateTime(2010, 1, 1),
                GroupeId = groupe.Id
            };
            db.Scouts.Add(chef);

            db.Branches.Add(new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Louveteaux",
                GroupeId = groupe.Id,
                ChefUniteId = chef.Id,
                NomChefUnite = "Alpha Chef"
            });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var createHtml = await client.GetStringAsync("/Branches/Create");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(createHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Branches/Create");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nom"] = " Louveteaux ",
            ["Description"] = "Branche test",
            ["AgeMin"] = "8",
            ["AgeMax"] = "12",
            ["GroupeId"] = groupe.Id.ToString(),
            ["ChefUniteId"] = chef.Id.ToString()
        });

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Une branche avec ce nom");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Branches.Count().Should().Be(1);
    }

    [Fact]
    public async Task Edit_Rejects_ChefUnite_From_Another_Groupe()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupeA = null!;
        Groupe groupeB = null!;
        Branche branche = null!;
        Scout chefA = null!;
        Scout chefB = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupeA = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe A" };
            groupeB = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe B" };
            db.Groupes.AddRange(groupeA, groupeB);

            chefA = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583756X",
                Nom = "Chef",
                Prenom = "Alpha",
                DateNaissance = new DateTime(2010, 1, 1),
                GroupeId = groupeA.Id
            };

            chefB = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583757X",
                Nom = "Chef",
                Prenom = "Beta",
                DateNaissance = new DateTime(2010, 1, 1),
                GroupeId = groupeB.Id
            };

            db.Scouts.AddRange(chefA, chefB);

            branche = new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Louveteaux",
                GroupeId = groupeA.Id,
                ChefUniteId = chefA.Id,
                NomChefUnite = "Alpha Chef"
            };
            db.Branches.Add(branche);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var editHtml = await client.GetStringAsync($"/Branches/Edit/{branche.Id}");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(editHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/Branches/Edit/{branche.Id}");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nom"] = "Louveteaux",
            ["Description"] = "Branche test",
            ["AgeMin"] = "8",
            ["AgeMax"] = "12",
            ["GroupeId"] = groupeA.Id.ToString(),
            ["ChefUniteId"] = chefB.Id.ToString()
        });

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("doit appartenir au groupe");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedBranche = await db.Branches.FindAsync(branche.Id);
        persistedBranche.Should().NotBeNull();
        persistedBranche!.ChefUniteId.Should().Be(chefA.Id);
    }

    [Fact]
    public async Task Details_Displays_Branche_Logo_When_Available()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupe = null!;
        Groupe otherGroupe = null!;
        Branche branche = null!;
        Branche otherBranche = null!;
        Scout chef = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe A", LogoUrl = "/uploads/groupes/a.png" };
            otherGroupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe B", LogoUrl = "/uploads/groupes/b.png" };
            chef = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583991X",
                Nom = "Yao",
                Prenom = "Edgar",
                PhotoUrl = "/uploads/scouts/edgar.png",
                DateNaissance = DateTime.UtcNow.AddYears(-25),
                GroupeId = groupe.Id
            };
            branche = new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Eclaireur",
                GroupeId = groupe.Id,
                LogoUrl = "/uploads/branches/eclaireur.png",
                ChefUniteId = chef.Id,
                NomChefUnite = "Edgar Yao"
            };
            otherBranche = new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Eclaireur",
                GroupeId = otherGroupe.Id
            };

            db.Groupes.AddRange(groupe, otherGroupe);
            db.Scouts.Add(chef);
            db.Branches.AddRange(branche, otherBranche);
            db.Scouts.AddRange(
                new Scout
                {
                    Id = Guid.NewGuid(),
                    GroupeId = groupe.Id,
                    BrancheId = branche.Id,
                    Matricule = "0583992X",
                    Nom = "Awa",
                    Prenom = "Jeune",
                    DateNaissance = DateTime.UtcNow.AddYears(-13),
                    Sexe = "Feminin",
                    Fonction = "CP"
                },
                new Scout
                {
                    Id = Guid.NewGuid(),
                    GroupeId = otherGroupe.Id,
                    BrancheId = otherBranche.Id,
                    Matricule = "0583993X",
                    Nom = "Jean",
                    Prenom = "Adulte",
                    DateNaissance = DateTime.UtcNow.AddYears(-20),
                    Sexe = "Masculin",
                    Fonction = "Assistant"
                });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync($"/Branches/Details/{branche.Id}");

        html.Should().Contain("/uploads/branches/eclaireur.png");
        html.Should().Contain("Logo de la branche");
        html.Should().Contain("Responsable de branche");
        html.Should().Contain("/uploads/scouts/edgar.png");
        html.Should().Contain("Total par groupe");
        html.Should().Contain("Groupe A");
        html.Should().Contain("Groupe B");
        html.Should().Contain("Liste des scouts et adultes de la branche");
        html.Should().Contain("Matricule");
        html.Should().Contain("Fonction");
    }
}
