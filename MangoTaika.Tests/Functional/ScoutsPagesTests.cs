using System.Net;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class ScoutsPagesTests
{
    [Fact]
    public async Task Create_Rejects_Branche_From_Another_Groupe()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupeA = null!;
        Groupe groupeB = null!;
        Branche brancheB = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupeA = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe A" };
            groupeB = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe B" };
            brancheB = new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Eclaireurs",
                GroupeId = groupeB.Id
            };

            db.Groupes.AddRange(groupeA, groupeB);
            db.Branches.Add(brancheB);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var createHtml = await client.GetStringAsync("/Scouts/Create");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(createHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Scouts/Create");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Matricule"] = "0583760X",
            ["Nom"] = "Kone",
            ["Prenom"] = "Awa",
            ["DateNaissance"] = "2012-05-14",
            ["GroupeId"] = groupeA.Id.ToString(),
            ["BrancheId"] = brancheB.Id.ToString()
        });

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("doit appartenir au groupe selectionne");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Scouts.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_Rejects_Duplicate_Matricule()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583761X",
                Nom = "Premier",
                Prenom = "Scout",
                DateNaissance = new DateTime(2011, 1, 1)
            });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var createHtml = await client.GetStringAsync("/Scouts/Create");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(createHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Scouts/Create");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Matricule"] = "0583761X",
            ["Nom"] = "Second",
            ["Prenom"] = "Scout",
            ["DateNaissance"] = "2012-05-14"
        });

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Le matricule existe deja");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Scouts.Count().Should().Be(1);
    }

    [Fact]
    public async Task ImportExcel_Invalid_Workbook_Shows_User_Friendly_Error()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var indexHtml = await client.GetStringAsync("/Scouts");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(indexHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Scouts/ImportExcel");
        request.Headers.Add("RequestVerificationToken", token);

        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("ceci n'est pas un vrai fichier xlsx")), "fichier", "import-corrompu.xlsx");
        request.Content = content;

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();

        var html = await client.GetStringAsync(response.Headers.Location);

        html.Should().Contain("fichier Excel (.xlsx) valide");
        html.Should().Contain("modele Excel");
    }

    [Fact]
    public async Task ImportExcel_Creates_Valid_Scouts_And_Shows_Summary_For_Invalid_Lines()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var indexHtml = await client.GetStringAsync("/Scouts");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(indexHtml);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");
        worksheet.Cell(1, 1).Value = "Matricule";
        worksheet.Cell(1, 2).Value = "Nom";
        worksheet.Cell(1, 3).Value = "Prenom";
        worksheet.Cell(1, 4).Value = "DateNaissance";

        worksheet.Cell(2, 1).Value = "0583772X";
        worksheet.Cell(2, 2).Value = "Kone";
        worksheet.Cell(2, 3).Value = "Awa";
        worksheet.Cell(2, 4).Value = new DateTime(2012, 5, 14);

        worksheet.Cell(3, 1).Value = "0583772X";
        worksheet.Cell(3, 2).Value = "Doublon";
        worksheet.Cell(3, 3).Value = "Scout";
        worksheet.Cell(3, 4).Value = new DateTime(2012, 6, 1);

        using var workbookStream = new MemoryStream();
        workbook.SaveAs(workbookStream);
        workbookStream.Position = 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Scouts/ImportExcel");
        request.Headers.Add("RequestVerificationToken", token);

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(workbookStream), "fichier", "import-scouts.xlsx");
        request.Content = content;

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();

        var html = await client.GetStringAsync(response.Headers.Location);

        html.Should().Contain("1 scout(s) cree(s) et 0 scout(s) mis a jour.");
        html.Should().Contain("1 cree(s), 0 mis a jour, 1 non enregistre(s).");
        html.Should().Contain("Matricule duplique dans le fichier");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Scouts.Should().ContainSingle(s => s.Matricule == "0583772X" && s.Nom == "Kone" && s.Prenom == "Awa");
    }

    [Fact]
    public async Task ImportExcel_Updates_Existing_Scout_And_Continues_On_NumeroCarte_Conflict()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Guid existingScoutId = Guid.Empty;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            var existingScout = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583774X",
                Nom = "Ancien",
                Prenom = "Scout",
                DateNaissance = new DateTime(2011, 1, 1),
                District = "District historique"
            };

            db.Scouts.Add(existingScout);
            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583775X",
                Nom = "Reserve",
                Prenom = "Carte",
                DateNaissance = new DateTime(2010, 2, 2),
                NumeroCarte = "ASCCI-999"
            });

            existingScoutId = existingScout.Id;
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var indexHtml = await client.GetStringAsync("/Scouts");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(indexHtml);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");
        worksheet.Cell(1, 1).Value = "Matricule";
        worksheet.Cell(1, 2).Value = "Nom";
        worksheet.Cell(1, 3).Value = "Prenom";
        worksheet.Cell(1, 4).Value = "DateNaissance";
        worksheet.Cell(1, 5).Value = "NumeroCarte";

        worksheet.Cell(2, 1).Value = "0583774X";
        worksheet.Cell(2, 2).Value = "Kone";
        worksheet.Cell(2, 3).Value = "Awa";
        worksheet.Cell(2, 4).Value = new DateTime(2012, 5, 14);

        worksheet.Cell(3, 1).Value = "0583776X";
        worksheet.Cell(3, 2).Value = "Doumbia";
        worksheet.Cell(3, 3).Value = "Lina";
        worksheet.Cell(3, 4).Value = new DateTime(2013, 6, 10);
        worksheet.Cell(3, 5).Value = "ASCCI-999";

        worksheet.Cell(4, 1).Value = "0583777X";
        worksheet.Cell(4, 2).Value = "Yao";
        worksheet.Cell(4, 3).Value = "Kevin";
        worksheet.Cell(4, 4).Value = new DateTime(2014, 7, 11);
        worksheet.Cell(4, 5).Value = "ASCCI-777";

        using var workbookStream = new MemoryStream();
        workbook.SaveAs(workbookStream);
        workbookStream.Position = 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Scouts/ImportExcel");
        request.Headers.Add("RequestVerificationToken", token);

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(workbookStream), "fichier", "import-scouts-upsert.xlsx");
        request.Content = content;

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();

        var html = await client.GetStringAsync(response.Headers.Location);

        html.Should().Contain("1 scout(s) cree(s) et 1 scout(s) mis a jour.");
        html.Should().Contain("1 cree(s), 1 mis a jour, 1 non enregistre(s).");
        html.Should().Contain("Numero de carte deja existant");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Scouts.Should().Contain(s => s.Matricule == "0583777X" && s.NumeroCarte == "ASCCI-777");
        db.Scouts.Should().Contain(s =>
            s.Id == existingScoutId &&
            s.Nom == "Kone" &&
            s.Prenom == "Awa" &&
            s.District == "District historique");
    }

    [Fact]
    public async Task ImportExcel_Limits_Displayed_Errors_To_Avoid_Too_Large_Response_Headers()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var indexHtml = await client.GetStringAsync("/Scouts");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(indexHtml);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");
        worksheet.Cell(1, 1).Value = "Matricule";
        worksheet.Cell(1, 2).Value = "Nom";
        worksheet.Cell(1, 3).Value = "Prenom";
        worksheet.Cell(1, 4).Value = "DateNaissance";

        worksheet.Cell(2, 1).Value = "0583773X";
        worksheet.Cell(2, 2).Value = "Kone";
        worksheet.Cell(2, 3).Value = "Awa";
        worksheet.Cell(2, 4).Value = new DateTime(2012, 5, 14);

        for (var row = 3; row <= 7; row++)
        {
            worksheet.Cell(row, 1).Value = "0583773X";
            worksheet.Cell(row, 2).Value = $"Doublon{row}";
            worksheet.Cell(row, 3).Value = "Scout";
            worksheet.Cell(row, 4).Value = new DateTime(2012, 6, 1);
        }

        using var workbookStream = new MemoryStream();
        workbook.SaveAs(workbookStream);
        workbookStream.Position = 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Scouts/ImportExcel");
        request.Headers.Add("RequestVerificationToken", token);

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(workbookStream), "fichier", "import-scouts-erreurs.xlsx");
        request.Content = content;

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();

        var html = await client.GetStringAsync(response.Headers.Location);

        html.Should().Contain("1 cree(s), 0 mis a jour, 5 non enregistre(s).");
        html.Should().Contain("Affichage limite aux 3 premieres erreurs.");
    }
}
