using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Integration;

public sealed class ScoutServiceIntegrationTests
{
    [Fact]
    public async Task CreateAsync_Persists_Normalized_Matricule_And_Assignment()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe A" };
        var branche = new Branche { Id = Guid.NewGuid(), Nom = "Eclaireurs", GroupeId = groupe.Id };

        db.Groupes.Add(groupe);
        db.Branches.Add(branche);
        await db.SaveChangesAsync();

        var service = new ScoutService(db);

        var created = await service.CreateAsync(new ScoutCreateDto
        {
            Matricule = "0583764x",
            Nom = " Kone ",
            Prenom = " Awa ",
            DateNaissance = new DateTime(2012, 5, 14),
            NumeroCarte = " ASCCI-001 ",
            Fonction = " Scout ",
            GroupeId = groupe.Id,
            BrancheId = branche.Id
        });

        var scout = await db.Scouts.SingleAsync();

        created.Matricule.Should().Be("0583764X");
        created.Nom.Should().Be("Kone");
        created.Prenom.Should().Be("Awa");
        scout.Matricule.Should().Be("0583764X");
        scout.NumeroCarte.Should().Be("ASCCI-001");
        scout.Fonction.Should().Be("Scout");
        scout.GroupeId.Should().Be(groupe.Id);
        scout.BrancheId.Should().Be(branche.Id);
    }

    [Fact]
    public async Task CreateAsync_Rejects_Duplicate_NumeroCarte()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        db.Scouts.Add(new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583765X",
            Nom = "Premier",
            Prenom = "Scout",
            DateNaissance = new DateTime(2011, 1, 1),
            NumeroCarte = "ASCCI-001"
        });
        await db.SaveChangesAsync();

        var service = new ScoutService(db);

        Func<Task> act = () => service.CreateAsync(new ScoutCreateDto
        {
            Matricule = "0583766X",
            Nom = "Second",
            Prenom = "Scout",
            DateNaissance = new DateTime(2012, 1, 1),
            NumeroCarte = " ascci-001 "
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Le numero de carte existe deja.*");

        db.Scouts.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_Finds_Scout_CaseInsensitively()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var scout = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583767X",
            Nom = "Kone",
            Prenom = "Awa",
            DateNaissance = new DateTime(2012, 6, 1),
            NumeroCarte = "ASCCI-777",
            District = "Abidjan Nord"
        };
        db.Scouts.Add(scout);
        await db.SaveChangesAsync();

        var service = new ScoutService(db);

        var byName = await service.SearchAsync("kOnE");
        var byMatricule = await service.SearchAsync("0583767x");

        byName.Should().ContainSingle(s => s.Id == scout.Id);
        byMatricule.Should().ContainSingle(s => s.Id == scout.Id);
    }

    [Fact]
    public async Task SearchAsync_Finds_Scout_Without_Requiring_Accents_Or_Apostrophes()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var scout = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583769X",
            Nom = "Cote",
            Prenom = "N'Guessan",
            DateNaissance = new DateTime(2011, 4, 8),
            District = "Côte d'Abidjan"
        };
        db.Scouts.Add(scout);
        await db.SaveChangesAsync();

        var service = new ScoutService(db);

        var byDistrict = await service.SearchAsync("cote dabidjan");
        var byPrenom = await service.SearchAsync("nguessan");

        byDistrict.Should().ContainSingle(s => s.Id == scout.Id);
        byPrenom.Should().ContainSingle(s => s.Id == scout.Id);
    }
}
