using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class DemandesPagesTests
{
    [Fact]
    public async Task Index_ShowsOnlyCurrentUsersDemandes_ForScout()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        ApplicationUser otherScout = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Aya", "Kone", ["Scout"]);
            otherScout = await TestDataSeeder.AddUserAsync(db, "Rokia", "Yao", ["Scout"]);

            db.DemandesAutorisation.AddRange(
                CreateDemande("Camp Alpha", scoutUser.Id),
                CreateDemande("Camp Beta", otherScout.Id));
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Demandes");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Demandes");
        html.Should().Contain("Camp Alpha");
        html.Should().NotContain("Camp Beta");
    }

    [Fact]
    public async Task Create_ReturnsForbidden_ForScoutWithoutChefRoleTag()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Lassina", "Scout", ["Scout"]);
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Demandes/Create");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_LoadsForScoutChef()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Moussa", "Chef", ["Scout"]);
            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "7000201B",
                Nom = "Chef",
                Prenom = "Moussa",
                Fonction = "Chef de troupe",
                DateNaissance = new DateTime(2000, 1, 15),
                IsActive = true
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Demandes/Create");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Nouvelle");
        html.Should().Contain("demande d'autorisation");
    }

    [Fact]
    public async Task Details_ReturnsForbidden_ForOtherUsersDemande()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        DemandeAutorisation otherDemande = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Ami", "Scout", ["Scout"]);
            var otherUser = await TestDataSeeder.AddUserAsync(db, "Kadi", "Scout", ["Scout"]);
            otherDemande = CreateDemande("Demande privee", otherUser.Id);
            db.DemandesAutorisation.Add(otherDemande);
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/Demandes/Details/{otherDemande.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static DemandeAutorisation CreateDemande(string titre, Guid demandeurId)
    {
        return new DemandeAutorisation
        {
            Id = Guid.NewGuid(),
            Titre = titre,
            Description = $"Description {titre}",
            TypeActivite = TypeActiviteDemande.Camp,
            DateActivite = DateTime.UtcNow.Date.AddDays(7),
            NombreParticipants = 24,
            DemandeurId = demandeurId,
            Statut = StatutDemande.Initialisee,
            DateCreation = DateTime.UtcNow.AddHours(-1)
        };
    }
}
