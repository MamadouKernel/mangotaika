using System.Text.RegularExpressions;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class FinancesPagesTests
{
    [Fact]
    public async Task Index_Displays_Report_A_Nouveau_And_Searchable_Scout_Selection()
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
                Nom = "Groupe Riviera"
            };

            db.Groupes.Add(groupe);
            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0584201X",
                Nom = "Kone",
                Prenom = "Awa",
                DateNaissance = new DateTime(2012, 5, 14),
                GroupeId = groupe.Id
            });

            db.TransactionsFinancieres.AddRange(
                new TransactionFinanciere
                {
                    Id = Guid.NewGuid(),
                    Libelle = "Subvention 2025",
                    Montant = 10000,
                    Type = TypeTransaction.Recette,
                    Categorie = CategorieFinance.Subvention,
                    DateTransaction = new DateTime(2025, 6, 10),
                    CreateurId = gestionnaire.Id
                },
                new TransactionFinanciere
                {
                    Id = Guid.NewGuid(),
                    Libelle = "Depense 2025",
                    Montant = 4000,
                    Type = TypeTransaction.Depense,
                    Categorie = CategorieFinance.Materiel,
                    DateTransaction = new DateTime(2025, 8, 12),
                    CreateurId = gestionnaire.Id
                },
                new TransactionFinanciere
                {
                    Id = Guid.NewGuid(),
                    Libelle = "Recette 2026",
                    Montant = 2500,
                    Type = TypeTransaction.Recette,
                    Categorie = CategorieFinance.Don,
                    DateTransaction = new DateTime(2026, 1, 15),
                    CreateurId = gestionnaire.Id
                });
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync("/Finances?annee=2026");

        html.Should().Contain("Report a nouveau");
        html.Should().Contain("Solde reporte depuis 2025");
        html.Should().Contain("6&#x202F;000 FCFA");
        html.Should().Contain("Solde disponible");
        html.Should().Contain("Rechercher un scout");
        html.Should().Contain("data-searchable-select");
        html.Should().Contain("Awa Kone (0584201X)");
    }

    [Fact]
    public async Task Edit_Displays_Searchable_Scout_Selection()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser gestionnaire = null!;
        Groupe groupe = null!;
        Scout scout = null!;
        TransactionFinanciere transaction = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Gestionnaire");
            gestionnaire = await TestDataSeeder.AddUserAsync(db, "Awa", "Gestion", ["Gestionnaire"]);

            groupe = new Groupe
            {
                Id = Guid.NewGuid(),
                Nom = "Groupe Riviera"
            };
            scout = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = "0584202X",
                Nom = "Yao",
                Prenom = "Kevin",
                DateNaissance = new DateTime(2011, 3, 4),
                GroupeId = groupe.Id
            };
            transaction = new TransactionFinanciere
            {
                Id = Guid.NewGuid(),
                Libelle = "Cotisation",
                Montant = 5000,
                Type = TypeTransaction.Recette,
                Categorie = CategorieFinance.Cotisation,
                DateTransaction = new DateTime(2026, 2, 15),
                GroupeId = groupe.Id,
                ScoutId = scout.Id,
                CreateurId = gestionnaire.Id
            };

            db.Groupes.Add(groupe);
            db.Scouts.Add(scout);
            db.TransactionsFinancieres.Add(transaction);
        });

        using var client = factory.CreateAuthenticatedClient(gestionnaire.Id, "Gestionnaire");

        var html = await client.GetStringAsync($"/Finances/Edit/{transaction.Id}");

        html.Should().Contain("Rechercher un scout");
        html.Should().Contain("financeEditGroupeSelect");
        html.Should().Contain("data-filter-group-select=\"financeEditGroupeSelect\"");
        html.Should().Contain("Kevin Yao (0584202X)");
        html.Should().MatchRegex($"<option value=\"{Regex.Escape(scout.Id.ToString())}\"[\\s\\S]*selected=");
    }
}
