using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class GroupesPagesTests
{
    [Fact]
    public async Task Index_Displays_Entities_Title_And_Chef_Contact()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        ApplicationUser responsable = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
            responsable = await TestDataSeeder.AddUserAsync(db, "Lucette", "Ossuh", []);
            responsable.PhoneNumber = "0701020304";

            db.Groupes.Add(new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "LES AIHES",
                NomChefGroupe = "OSSUH Lucette",
                ResponsableId = responsable.Id
            });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync("/Groupes");

        html.Should().Contain("Entit&eacute;s");
        html.Should().Contain("Scouts du District");
        html.Should().Contain("Entites scouts");
        html.Should().Contain("Chef de groupe : OSSUH Lucette");
        html.Should().Contain("Contact : 0701020304");
    }

    [Fact]
    public async Task Create_And_Edit_Display_Responsable_Field_And_Current_Selection()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        ApplicationUser responsable = null!;
        Groupe groupe = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
            responsable = await TestDataSeeder.AddUserAsync(db, "Lucette", "Ossuh", []);
            responsable.PhoneNumber = "0701020304";

            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "Groupe Riviera",
                ResponsableId = responsable.Id
            };

            db.Groupes.Add(groupe);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var createHtml = await client.GetStringAsync("/Groupes/Create");
        createHtml.Should().Contain("Responsable rattache");
        createHtml.Should().Contain("Le contact et la photo du responsable sur la fiche detail proviennent de cet utilisateur.");
        createHtml.Should().Contain("Lucette Ossuh");
        createHtml.Should().Contain("0701020304");

        var editHtml = await client.GetStringAsync($"/Groupes/Edit/{groupe.Id}");
        editHtml.Should().Contain("Responsable rattache");
        editHtml.Should().Contain("Lucette Ossuh");
        editHtml.Should().MatchRegex($"<option value=\"{Regex.Escape(responsable.Id.ToString())}\" selected=\"selected\">");
    }

    [Fact]
    public async Task Create_Rejects_Duplicate_Name()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
            db.Groupes.Add(new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "Groupe Riviera"
            });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var createHtml = await client.GetStringAsync("/Groupes/Create");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(createHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Groupes/Create");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nom"] = " Groupe Riviera ",
            ["Commune"] = "Cocody",
            ["Quartier"] = "Riviera 2"
        });

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Un groupe avec ce nom existe deja");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Groupes.Count().Should().Be(1);
    }

    [Fact]
    public async Task Details_Displays_Group_Logo_When_Available()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        ApplicationUser responsable = null!;
        Groupe groupe = null!;
        Branche brancheJeunes = null!;
        Branche brancheAdultes = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
            responsable = await TestDataSeeder.AddUserAsync(db, "Lucette", "Ossuh", []);
            responsable.PhoneNumber = "0701020304";
            responsable.PhotoUrl = "/uploads/users/lucette.png";
            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "Groupe Riviera",
                LogoUrl = "/uploads/groupes/riviera.png",
                NomChefGroupe = "OSSUH Lucette",
                ResponsableId = responsable.Id
            };
            brancheJeunes = new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Oisillons",
                GroupeId = groupe.Id
            };
            brancheAdultes = new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Routier",
                GroupeId = groupe.Id
            };
            db.Groupes.Add(groupe);
            db.Branches.AddRange(brancheJeunes, brancheAdultes);
            db.Scouts.AddRange(
                new Scout
                {
                    Id = Guid.NewGuid(),
                    GroupeId = groupe.Id,
                    BrancheId = brancheJeunes.Id,
                    Matricule = "0583001A",
                    Nom = "Aka",
                    Prenom = "JeuneF",
                    DateNaissance = DateTime.UtcNow.AddYears(-12),
                    Sexe = "Feminin"
                },
                new Scout
                {
                    Id = Guid.NewGuid(),
                    GroupeId = groupe.Id,
                    BrancheId = brancheAdultes.Id,
                    Matricule = "0583002B",
                    Nom = "Kouame",
                    Prenom = "AdulteM",
                    DateNaissance = DateTime.UtcNow.AddYears(-28),
                    Sexe = "Masculin"
                });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync($"/Groupes/Details/{groupe.Id}");

        html.Should().Contain("/uploads/groupes/riviera.png");
        html.Should().Contain("Logo du groupe");
        html.Should().Contain("Responsable");
        html.Should().Contain("/uploads/users/lucette.png");
        html.Should().Contain("0701020304");
        html.Should().Contain("Totaux par branche");
        html.Should().Contain("Oisillons");
        html.Should().Contain("Routier");
        html.Should().Contain("Scouts");
        html.Should().Contain("Jeunes");
        html.Should().Contain("Adultes");
        html.Should().Contain("1 filles | 0 garcons");
        html.Should().Contain("0 filles | 1 garcons");
        html.Should().Contain("F 1 / G 0");
        html.Should().Contain("F 0 / G 1");
    }

    [Fact]
    public async Task Create_New_Group_Inherits_Branches_From_District_Group()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            var districtGroup = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "Equipe de District Mango Taika"
            };

            db.Groupes.Add(districtGroup);
            db.Branches.AddRange(
                new Branche
                {
                    Id = Guid.NewGuid(),
                    Nom = "Louveteau",
                    GroupeId = districtGroup.Id,
                    AgeMin = 8,
                    AgeMax = 12
                },
                new Branche
                {
                    Id = Guid.NewGuid(),
                    Nom = "Eclaireur",
                    GroupeId = districtGroup.Id,
                    AgeMin = 12,
                    AgeMax = 14
                });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var createHtml = await client.GetStringAsync("/Groupes/Create");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(createHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Groupes/Create");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nom"] = "LES PITIKAS",
            ["Commune"] = "Cocody",
            ["Quartier"] = "Riviera"
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var groupe = await db.Groupes.SingleAsync(g => g.Nom == "LES PITIKAS");
        var branches = await db.Branches
            .Where(b => b.GroupeId == groupe.Id && b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();

        branches.Should().HaveCount(2);
        branches.Select(b => b.Nom).Should().BeEquivalentTo(["Eclaireur", "Louveteau"]);
    }
}
