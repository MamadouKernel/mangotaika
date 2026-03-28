using System.Net;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
        html.Should().Contain("Chef de groupe : OSSUH Lucette");
        html.Should().Contain("Contact : 0701020304");
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
        Groupe groupe = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);
            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "Groupe Riviera",
                LogoUrl = "/uploads/groupes/riviera.png"
            };
            db.Groupes.Add(groupe);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync($"/Groupes/Details/{groupe.Id}");

        html.Should().Contain("/uploads/groupes/riviera.png");
        html.Should().Contain("Logo du groupe");
    }
}
