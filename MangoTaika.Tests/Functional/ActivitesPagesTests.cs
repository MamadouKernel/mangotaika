using System.Net;
using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class ActivitesPagesTests
{
    [Fact]
    public async Task Details_AllowsChefGroupeLinkedByScoutFunction_ToManageParticipants()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser chefGroupeUser = null!;
        Activite activite = null!;
        Scout participant = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            chefGroupeUser = await TestDataSeeder.AddUserAsync(db, "Joseph", "Kpolo", ["Scout"]);

            var groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "LES KORES MOANAS",
                IsActive = true,
                DateCreation = DateTime.UtcNow
            };
            db.Groupes.Add(groupe);

            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                UserId = chefGroupeUser.Id,
                Matricule = "0547834X",
                Nom = "KPOLO",
                Prenom = "Joseph",
                Fonction = "Chef de Groupe",
                GroupeId = groupe.Id,
                DateNaissance = new DateTime(1990, 1, 1),
                IsActive = true
            });

            participant = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0583753X",
                Nom = "ADJOUMANI",
                Prenom = "Ezechiel",
                GroupeId = groupe.Id,
                DateNaissance = new DateTime(2005, 1, 1),
                IsActive = true
            };
            db.Scouts.Add(participant);

            activite = new Activite
            {
                Id = Guid.NewGuid(),
                Titre = "Test appli",
                Type = TypeActivite.Autre,
                DateDebut = DateTime.UtcNow.Date.AddDays(3),
                GroupeId = groupe.Id,
                CreateurId = chefGroupeUser.Id,
                Statut = StatutActivite.Validee
            };
            db.Activites.Add(activite);
        });

        using var client = factory.CreateAuthenticatedClient(chefGroupeUser.Id, "Scout");
        var detailsHtml = await client.GetStringAsync($"/Activites/Details/{activite.Id}");

        detailsHtml.Should().Contain("Ajouter les participants");
        detailsHtml.Should().Contain("0583753X");

        var token = HtmlTestHelpers.ExtractAntiForgeryToken(detailsHtml);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["scoutIds"] = participant.Id.ToString()
        });

        var response = await client.PostAsync($"/Activites/AjouterParticipants/{activite.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = db.ParticipantsActivite.Any(p =>
            p.ActiviteId == activite.Id
            && p.ScoutId == participant.Id
            && !p.EstSupprime);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Details_KeepsOrdinaryScoutReadOnly_ForScopedActivity()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;
        Activite activite = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Aya", "Scout", ["Scout"]);

            var groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "LES KORES MOANAS",
                IsActive = true,
                DateCreation = DateTime.UtcNow
            };
            db.Groupes.Add(groupe);

            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "0583754X",
                Nom = "Scout",
                Prenom = "Aya",
                GroupeId = groupe.Id,
                DateNaissance = new DateTime(2005, 1, 1),
                IsActive = true
            });

            activite = new Activite
            {
                Id = Guid.NewGuid(),
                Titre = "Activite groupe",
                Type = TypeActivite.Autre,
                DateDebut = DateTime.UtcNow.Date.AddDays(3),
                GroupeId = groupe.Id,
                CreateurId = scoutUser.Id,
                Statut = StatutActivite.Validee
            };
            db.Activites.Add(activite);
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync($"/Activites/Details/{activite.Id}");
        var detailsHtml = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        detailsHtml.Should().NotContain("Ajouter les participants");
    }

    [Fact]
    public async Task Index_AllowsCommissaireDistrict_ToSoftDeleteActivity()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser commissaire = null!;
        Activite activite = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "CommissaireDistrict");
            commissaire = await TestDataSeeder.AddUserAsync(db, "Mango", "Admin", ["CommissaireDistrict"]);

            activite = new Activite
            {
                Id = Guid.NewGuid(),
                Titre = "Activite a supprimer",
                Type = TypeActivite.Autre,
                DateDebut = DateTime.UtcNow.Date.AddDays(3),
                CreateurId = commissaire.Id,
                Statut = StatutActivite.Validee
            };
            db.Activites.Add(activite);
        });

        using var client = factory.CreateAuthenticatedClient(commissaire.Id, "CommissaireDistrict");
        var indexHtml = await client.GetStringAsync("/Activites");

        indexHtml.Should().Contain("Activite a supprimer");
        indexHtml.Should().Contain($"/Activites/Delete/{activite.Id}");

        var token = HtmlTestHelpers.ExtractAntiForgeryToken(indexHtml);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync($"/Activites/Delete/{activite.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var listAfterDelete = await client.GetStringAsync("/Activites");
        listAfterDelete.Should().NotContain("Activite a supprimer");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedActivity = await db.Activites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == activite.Id);

        persistedActivity.Should().NotBeNull();
        persistedActivity!.EstSupprime.Should().BeTrue();
    }
}
