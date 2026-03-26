using System.Net;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class DemandesGroupePagesTests
{
    [Fact]
    public async Task Creer_PageLoadsAnonymously()
    {
        await using var factory = new SupportWebApplicationFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/DemandesGroupe/Creer");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Soumettre la demande");
        html.Should().Contain("Nom du groupe");
    }

    [Fact]
    public async Task Index_LoadsForConsultant_WithoutManagementActions()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser consultant = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Consultant");
            consultant = await TestDataSeeder.AddUserAsync(db, "Jean", "Consultant", ["Consultant"]);
            db.DemandesGroupe.Add(new DemandeGroupe
            {
                Id = Guid.NewGuid(),
                NomGroupe = "Groupe Plateau",
                Commune = "Plateau",
                Quartier = "Commerce",
                NomResponsable = "Responsable Plateau",
                TelephoneResponsable = "0102030405",
                NombreMembresPrevus = 18
            });
        });

        using var client = factory.CreateAuthenticatedClient(consultant.Id, "Consultant");

        var response = await client.GetAsync("/DemandesGroupe");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Groupe Plateau");
        html.Should().NotContain("Approuver");
        html.Should().NotContain("Rejeter");
    }

    [Fact]
    public async Task Approuver_CreatesGroup_AndUpdatesDemande()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        DemandeGroupe demande = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Fatou", "Gestion", ["Gestionnaire"]);
            demande = new DemandeGroupe
            {
                Id = Guid.NewGuid(),
                NomGroupe = "Groupe Riviera",
                Commune = "Cocody",
                Quartier = "Riviera 2",
                NomResponsable = "Kouadio Yao",
                TelephoneResponsable = "0708091011",
                NombreMembresPrevus = 25
            };
            db.DemandesGroupe.Add(demande);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");
        var indexHtml = await client.GetStringAsync("/DemandesGroupe");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(indexHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/DemandesGroupe/Approuver?id={demande.Id}");
        request.Headers.Add("RequestVerificationToken", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedDemande = await db.DemandesGroupe.FindAsync(demande.Id);
        var createdGroup = db.Groupes.Single(g => g.Nom == "Groupe Riviera");

        persistedDemande.Should().NotBeNull();
        persistedDemande!.Statut.Should().Be(StatutDemandeGroupe.Approuvee);
        persistedDemande.TraiteParId.Should().Be(gestionnaire.Id);
        createdGroup.Adresse.Should().Be("Riviera 2, Cocody");
        createdGroup.Latitude.Should().Be(5.3364);
        createdGroup.Longitude.Should().Be(-4.0267);
    }
}
