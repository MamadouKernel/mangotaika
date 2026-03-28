using System.Text.RegularExpressions;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class AGRPagesTests
{
    [Fact]
    public async Task Index_Displays_Searchable_Responsable_Selection()
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
                Nom = "Equipe de District Mango Taika"
            };

            db.Groupes.Add(groupe);
            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0584301X",
                Nom = "Tie",
                Prenom = "Rodrigue",
                DateNaissance = new DateTime(2000, 1, 1),
                GroupeId = groupe.Id
            });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync("/AGR");

        html.Should().Contain("Rechercher un responsable");
        html.Should().Contain("data-searchable-select");
        html.Should().Contain("data-filter-group-select=\"agrGroupeSelect\"");
        html.Should().Contain("Rodrigue Tie (0584301X)");
    }

    [Fact]
    public async Task Edit_Preserves_Current_Responsable_And_Displays_Searchable_Selection()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupe = null!;
        ProjetAGR projet = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "LES AIHES"
            };

            db.Groupes.Add(groupe);
            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0584302X",
                Nom = "Sanogo",
                Prenom = "Drissa",
                DateNaissance = new DateTime(1999, 4, 7),
                GroupeId = groupe.Id
            });

            projet = new ProjetAGR
            {
                Id = Guid.NewGuid(),
                Nom = "Vente de pin's",
                BudgetInitial = 50000,
                DateDebut = new DateTime(2026, 3, 27),
                Responsable = "Rodrigue TIE",
                GroupeId = groupe.Id,
                CreateurId = gestionnaire.Id
            };

            db.ProjetsAGR.Add(projet);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync($"/AGR/Edit/{projet.Id}");

        html.Should().Contain("Rechercher un responsable");
        html.Should().Contain("data-filter-group-select=\"agrEditGroupeSelect\"");
        html.Should().Contain("Rodrigue TIE");
        html.Should().Contain("Drissa Sanogo (0584302X)");
        html.Should().MatchRegex($"<option value=\"{Regex.Escape(projet.Responsable!)}\" selected=\"selected\">");
    }
}
