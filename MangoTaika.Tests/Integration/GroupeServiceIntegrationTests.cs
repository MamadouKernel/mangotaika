using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Integration;

public sealed class GroupeServiceIntegrationTests
{
    [Fact]
    public async Task CreateAsync_Builds_Address_And_Uses_Geocoding_Result()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var service = new GroupeService(db, new FakeGeocodingService());

        var created = await service.CreateAsync(new GroupeCreateDto
        {
            Nom = " Groupe Riviera ",
            Description = " Groupe test ",
            LogoUrl = "/uploads/groupes/riviera.png",
            Commune = "Cocody",
            Quartier = "Riviera 2",
            NomChefGroupe = " Chef Principal ",
            Latitude = 1.5,
            Longitude = 2.5
        });

        var groupe = await db.Groupes.SingleAsync();

        created.Nom.Should().Be("Groupe Riviera");
        created.Adresse.Should().Be("Riviera 2, Cocody");
        created.LogoUrl.Should().Be("/uploads/groupes/riviera.png");
        created.Latitude.Should().Be(5.3364);
        created.Longitude.Should().Be(-4.0267);
        groupe.Description.Should().Be("Groupe test");
        groupe.LogoUrl.Should().Be("/uploads/groupes/riviera.png");
        groupe.NomChefGroupe.Should().Be("Chef Principal");
    }

    [Fact]
    public async Task CreateAsync_Rejects_Inactive_Responsable()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var responsable = await TestDataSeeder.AddUserAsync(db, "Awa", "Responsable", [], isActive: false);
        var service = new GroupeService(db, new FakeGeocodingService());

        Func<Task> act = () => service.CreateAsync(new GroupeCreateDto
        {
            Nom = "Groupe Riviera",
            ResponsableId = responsable.Id
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*responsable selectionne est introuvable ou inactif*");

        db.Groupes.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Rejects_Duplicate_Name_When_Accents_And_Apostrophes_Differ()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        db.Groupes.Add(new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = "Groupe Cote d'Ivoire"
        });
        await db.SaveChangesAsync();

        var service = new GroupeService(db, new FakeGeocodingService());

        Func<Task> act = () => service.CreateAsync(new GroupeCreateDto
        {
            Nom = "Groupe Côte d Ivoire"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Un groupe avec ce nom existe deja.*");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Detailed_Demographics_And_Branch_Totals()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var responsable = await TestDataSeeder.AddUserAsync(db, "Lucette", "Ossuh", []);
        responsable.PhoneNumber = "0701020304";
        responsable.PhotoUrl = "/uploads/users/lucette.png";

        var groupe = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = "LES AIHES",
            NomChefGroupe = "OSSUH Lucette",
            ResponsableId = responsable.Id
        };

        var oisillons = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Oisillons",
            GroupeId = groupe.Id
        };

        var routiers = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Routier",
            GroupeId = groupe.Id
        };

        db.Groupes.Add(groupe);
        db.Branches.AddRange(oisillons, routiers);
        db.Scouts.AddRange(
            CreateScout(groupe.Id, oisillons.Id, "0583001A", DateTime.UtcNow.AddYears(-12), "Feminin"),
            CreateScout(groupe.Id, oisillons.Id, "0583002B", DateTime.UtcNow.AddYears(-15), "Masculin"),
            CreateScout(groupe.Id, routiers.Id, "0583003C", DateTime.UtcNow.AddYears(-24), "Feminin"),
            CreateScout(groupe.Id, routiers.Id, "0583004D", DateTime.UtcNow.AddYears(-30), "Masculin"));
        await db.SaveChangesAsync();

        var service = new GroupeService(db, new FakeGeocodingService());

        var dto = await service.GetByIdAsync(groupe.Id);

        dto.Should().NotBeNull();
        dto!.ResponsablePhotoUrl.Should().Be("/uploads/users/lucette.png");
        dto.ContactChefGroupe.Should().Be("0701020304");
        dto.NombreMembres.Should().Be(4);
        dto.NombreFilles.Should().Be(2);
        dto.NombreGarcons.Should().Be(2);
        dto.Jeunes.Total.Should().Be(2);
        dto.Jeunes.NombreFeminin.Should().Be(1);
        dto.Jeunes.NombreMasculin.Should().Be(1);
        dto.Adultes.Total.Should().Be(2);
        dto.Adultes.NombreFeminin.Should().Be(1);
        dto.Adultes.NombreMasculin.Should().Be(1);

        dto.BranchesScouts.Should().HaveCount(2);
        dto.BranchesScouts.Should().ContainSingle(b =>
            b.Nom == "Oisillons" &&
            b.NombreScouts == 2 &&
            b.Jeunes.Total == 2 &&
            b.Adultes.Total == 0);
        dto.BranchesScouts.Should().ContainSingle(b =>
            b.Nom == "Routier" &&
            b.NombreScouts == 2 &&
            b.Jeunes.Total == 0 &&
            b.Adultes.Total == 2);
    }

    private static Scout CreateScout(Guid groupeId, Guid brancheId, string matricule, DateTime dateNaissance, string sexe)
    {
        return new Scout
        {
            Id = Guid.NewGuid(),
            GroupeId = groupeId,
            BrancheId = brancheId,
            Matricule = matricule,
            Nom = "Scout",
            Prenom = matricule,
            DateNaissance = dateNaissance,
            Sexe = sexe,
            IsActive = true
        };
    }
}
