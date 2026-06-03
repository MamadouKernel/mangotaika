using System.Net;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task Index_ShowsChefUniteSubmittedDemandes_ForChefGroupeLinkedByScoutProfile()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser chefGroupeUser = null!;
        ApplicationUser chefUniteUser = null!;
        Groupe groupe = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout", "ChefGroupe", "ChefUnite");
            chefGroupeUser = await TestDataSeeder.AddUserAsync(db, "Joseph", "ChefGroupe", ["Scout", "ChefGroupe"]);
            chefUniteUser = await TestDataSeeder.AddUserAsync(db, "Aya", "ChefUnite", ["Scout", "ChefUnite"]);

            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "LES KORES MOANAS",
                IsActive = true,
                DateCreation = DateTime.UtcNow
            };
            db.Groupes.Add(groupe);

            db.Scouts.AddRange(
                new Scout
                {
                    Id = Guid.NewGuid(),
                    UserId = chefGroupeUser.Id,
                    Matricule = "0547834X",
                    Nom = "ChefGroupe",
                    Prenom = "Joseph",
                    Fonction = "Chef de Groupe",
                    GroupeId = groupe.Id,
                    DateNaissance = new DateTime(1990, 1, 1),
                    IsActive = true
                },
                new Scout
                {
                    Id = Guid.NewGuid(),
                    UserId = chefUniteUser.Id,
                    Matricule = "0583753X",
                    Nom = "ChefUnite",
                    Prenom = "Aya",
                    Fonction = "Chef de troupe",
                    GroupeId = groupe.Id,
                    DateNaissance = new DateTime(2000, 1, 1),
                    IsActive = true
                });

            db.DemandesAutorisation.Add(new DemandeAutorisation
            {
                Id = Guid.NewGuid(),
                Titre = "Plantain Troup",
                Description = "Demande pour validation groupe",
                TypeActivite = TypeActiviteDemande.Sortie,
                DateActivite = DateTime.UtcNow.Date.AddDays(7),
                NombreParticipants = 26,
                DemandeurId = chefUniteUser.Id,
                GroupeId = groupe.Id,
                Statut = StatutDemande.Soumise,
                DateCreation = DateTime.UtcNow.AddHours(-1)
            });
        });

        using var client = factory.CreateAuthenticatedClient(chefGroupeUser.Id, "Scout");

        var response = await client.GetAsync("/Demandes");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Plantain Troup");
    }

    [Fact]
    public async Task Valider_ByChefGroupe_ValidatesNonCampDemand_AndCreatesActivity()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser chefGroupeUser = null!;
        ApplicationUser chefUniteUser = null!;
        DemandeAutorisation demande = null!;
        Groupe groupe = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout", "ChefGroupe", "ChefUnite");
            chefGroupeUser = await TestDataSeeder.AddUserAsync(db, "Joseph", "ChefGroupe", ["Scout", "ChefGroupe"]);
            chefUniteUser = await TestDataSeeder.AddUserAsync(db, "Aya", "ChefUnite", ["Scout", "ChefUnite"]);

            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "LES KORES MOANAS",
                IsActive = true,
                DateCreation = DateTime.UtcNow
            };
            db.Groupes.Add(groupe);

            db.Scouts.AddRange(
                new Scout
                {
                    Id = Guid.NewGuid(),
                    UserId = chefGroupeUser.Id,
                    Matricule = "0547834X",
                    Nom = "ChefGroupe",
                    Prenom = "Joseph",
                    Fonction = "Chef de Groupe",
                    GroupeId = groupe.Id,
                    DateNaissance = new DateTime(1990, 1, 1),
                    IsActive = true
                },
                new Scout
                {
                    Id = Guid.NewGuid(),
                    UserId = chefUniteUser.Id,
                    Matricule = "0583753X",
                    Nom = "ChefUnite",
                    Prenom = "Aya",
                    Fonction = "Chef de troupe",
                    GroupeId = groupe.Id,
                    DateNaissance = new DateTime(2000, 1, 1),
                    IsActive = true
                });

            demande = new DemandeAutorisation
            {
                Id = Guid.NewGuid(),
                Titre = "Alloco party",
                Description = "Demande non camp",
                TypeActivite = TypeActiviteDemande.Sortie,
                DateActivite = DateTime.UtcNow.Date.AddDays(7),
                NombreParticipants = 26,
                DemandeurId = chefUniteUser.Id,
                GroupeId = groupe.Id,
                Statut = StatutDemande.Soumise,
                DateCreation = DateTime.UtcNow.AddHours(-1)
            };
            db.DemandesAutorisation.Add(demande);
        });

        using var client = factory.CreateAuthenticatedClient(chefGroupeUser.Id, "Scout");
        var detailsHtml = await client.GetStringAsync($"/Demandes/Details/{demande.Id}");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(detailsHtml);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/Demandes/Valider?id={demande.Id}");
        request.Headers.Add("RequestVerificationToken", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedDemande = await db.DemandesAutorisation.FindAsync(demande.Id);
        var createdActivity = db.Activites.SingleOrDefault(a => a.Titre == "Alloco party");

        persistedDemande.Should().NotBeNull();
        persistedDemande!.Statut.Should().Be(StatutDemande.Validee);
        createdActivity.Should().NotBeNull();
        createdActivity!.Statut.Should().Be(StatutActivite.Validee);
        createdActivity.GroupeId.Should().Be(groupe.Id);
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
