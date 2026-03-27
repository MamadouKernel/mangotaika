using System.Text;
using ClosedXML.Excel;
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

    [Fact]
    public async Task ImportFromExcelAsync_Updates_Existing_Scout_When_Matricule_Exists()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var existingScout = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583770X",
            Nom = "Existant",
            Prenom = "Scout",
            DateNaissance = new DateTime(2011, 1, 1),
            District = "Abidjan Nord",
            NumeroCarte = "ASCCI-001"
        };
        db.Scouts.Add(existingScout);
        await db.SaveChangesAsync();

        var service = new ScoutService(db);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");

        worksheet.Cell(1, 1).Value = "Matricule";
        worksheet.Cell(1, 2).Value = "Nom";
        worksheet.Cell(1, 3).Value = "Prenom";
        worksheet.Cell(1, 4).Value = "DateNaissance";

        worksheet.Cell(1, 5).Value = "NumeroCarte";

        worksheet.Cell(2, 1).Value = "0583770X";
        worksheet.Cell(2, 2).Value = "Kone";
        worksheet.Cell(2, 3).Value = "Awa";
        worksheet.Cell(2, 4).Value = new DateTime(2012, 5, 14);
        worksheet.Cell(2, 5).Value = "ASCCI-002";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ImportFromExcelAsync(stream);

        result.CreatedCount.Should().Be(0);
        result.UpdatedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        result.UpdatedMatricules.Should().ContainSingle().Which.Should().Be("0583770X");
        result.Errors.Should().BeEmpty();
        db.Scouts.Should().HaveCount(1);

        var updatedScout = await db.Scouts.SingleAsync(s => s.Id == existingScout.Id);
        updatedScout.Nom.Should().Be("Kone");
        updatedScout.Prenom.Should().Be("Awa");
        updatedScout.DateNaissance.Should().Be(new DateTime(2012, 5, 14));
        updatedScout.NumeroCarte.Should().Be("ASCCI-002");
        updatedScout.District.Should().Be("Abidjan Nord");
    }

    [Fact]
    public async Task ImportFromExcelAsync_Skips_NumeroCarte_Conflict_And_Continues()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        db.Scouts.Add(new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583771X",
            Nom = "Premier",
            Prenom = "Scout",
            DateNaissance = new DateTime(2011, 1, 1),
            NumeroCarte = "ASCCI-001"
        });
        await db.SaveChangesAsync();

        var service = new ScoutService(db);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");

        worksheet.Cell(1, 1).Value = "Matricule";
        worksheet.Cell(1, 2).Value = "Nom";
        worksheet.Cell(1, 3).Value = "Prenom";
        worksheet.Cell(1, 4).Value = "DateNaissance";
        worksheet.Cell(1, 5).Value = "NumeroCarte";

        worksheet.Cell(2, 1).Value = "0583772X";
        worksheet.Cell(2, 2).Value = "Kone";
        worksheet.Cell(2, 3).Value = "Awa";
        worksheet.Cell(2, 4).Value = new DateTime(2012, 5, 14);
        worksheet.Cell(2, 5).Value = "ASCCI-001";

        worksheet.Cell(3, 1).Value = "0583773X";
        worksheet.Cell(3, 2).Value = "Yao";
        worksheet.Cell(3, 3).Value = "Kevin";
        worksheet.Cell(3, 4).Value = new DateTime(2013, 6, 10);
        worksheet.Cell(3, 5).Value = "ASCCI-002";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ImportFromExcelAsync(stream);

        result.CreatedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.CreatedMatricules.Should().ContainSingle().Which.Should().Be("0583773X");
        result.Errors.Should().ContainSingle(e =>
            e.LineNumber == 2 &&
            e.Matricule == "0583772X" &&
            e.Message.Contains("Numero de carte deja existant"));
        db.Scouts.Should().HaveCount(2);
        db.Scouts.Should().Contain(s => s.Matricule == "0583773X" && s.NumeroCarte == "ASCCI-002");
    }

    [Fact]
    public async Task ImportFromExcelAsync_Rejects_Invalid_Workbook_With_Explicit_Message()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var service = new ScoutService(db);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("ceci n'est pas un vrai fichier xlsx"));

        Func<Task> act = () => service.ImportFromExcelAsync(stream);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*fichier Excel (.xlsx) valide*modele Excel*");
    }
}
