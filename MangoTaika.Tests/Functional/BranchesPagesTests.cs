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
}
