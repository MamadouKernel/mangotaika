using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Integration;

public sealed class BrancheServiceIntegrationTests
{
    [Fact]
    public async Task CreateAsync_Persists_Normalized_Branche_And_ChefUnite()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Riviera" };
        var chef = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583762X",
            Nom = "Chef",
            Prenom = "Alpha",
            DateNaissance = new DateTime(2010, 1, 1),
            GroupeId = groupe.Id
        };

        db.Groupes.Add(groupe);
        db.Scouts.Add(chef);
        await db.SaveChangesAsync();

        var service = new BrancheService(db);

        var created = await service.CreateAsync(new BrancheCreateDto
        {
            Nom = " Louveteaux ",
            Description = " Branche des 8-12 ans ",
            AgeMin = 8,
            AgeMax = 12,
            GroupeId = groupe.Id,
            ChefUniteId = chef.Id
        });

        var branche = await db.Branches.Include(b => b.ChefUnite).SingleAsync();

        created.Nom.Should().Be("Louveteaux");
        created.NomChefUnite.Should().Be("Alpha Chef");
        branche.Nom.Should().Be("Louveteaux");
        branche.Description.Should().Be("Branche des 8-12 ans");
        branche.GroupeId.Should().Be(groupe.Id);
        branche.ChefUniteId.Should().Be(chef.Id);
        branche.NomChefUnite.Should().Be("Alpha Chef");
    }

    [Fact]
    public async Task CreateAsync_Rejects_Duplicate_Name_In_Same_Groupe()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Riviera" };
        var chef = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583763X",
            Nom = "Chef",
            Prenom = "Beta",
            DateNaissance = new DateTime(2010, 1, 1),
            GroupeId = groupe.Id
        };

        db.Groupes.Add(groupe);
        db.Scouts.Add(chef);
        db.Branches.Add(new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Louveteaux",
            GroupeId = groupe.Id,
            ChefUniteId = chef.Id,
            NomChefUnite = "Beta Chef"
        });
        await db.SaveChangesAsync();

        var service = new BrancheService(db);

        Func<Task> act = () => service.CreateAsync(new BrancheCreateDto
        {
            Nom = " louveteaux ",
            GroupeId = groupe.Id,
            ChefUniteId = chef.Id
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Une branche avec ce nom existe deja dans ce groupe.*");

        db.Branches.Should().HaveCount(1);
    }
}
