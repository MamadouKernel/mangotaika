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
        var inheritance = new DistrictBranchInheritanceService(db);
        var service = new GroupeService(db, new FakeGeocodingService(), inheritance);

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
        groupe.NomChefGroupe.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_Ignores_Obsolete_ResponsableId_On_Create()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var responsable = await TestDataSeeder.AddUserAsync(db, "Awa", "Responsable", [], isActive: false);
        var inheritance = new DistrictBranchInheritanceService(db);
        var service = new GroupeService(db, new FakeGeocodingService(), inheritance);

        var created = await service.CreateAsync(new GroupeCreateDto
        {
            Nom = "Groupe Riviera",
            ResponsableId = responsable.Id
        });

        created.Nom.Should().Be("Groupe Riviera");
        created.ResponsableId.Should().BeNull();
        db.Groupes.Should().ContainSingle();
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

        var inheritance = new DistrictBranchInheritanceService(db);
        var service = new GroupeService(db, new FakeGeocodingService(), inheritance);

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

        var inheritance = new DistrictBranchInheritanceService(db);
        var service = new GroupeService(db, new FakeGeocodingService(), inheritance);

        var dto = await service.GetByIdAsync(groupe.Id);

        dto.Should().NotBeNull();
        dto!.ResponsablePhotoUrl.Should().Be("/uploads/users/lucette.png");
        dto.ContactChefGroupe.Should().Be("0701020304");
        dto.NombreMembres.Should().Be(4);
        dto.NombreFilles.Should().Be(2);
        dto.NombreGarcons.Should().Be(2);
        dto.Jeunes.Total.Should().Be(4);
        dto.Jeunes.NombreFeminin.Should().Be(2);
        dto.Jeunes.NombreMasculin.Should().Be(2);
        dto.Adultes.Total.Should().Be(0);
        dto.Adultes.NombreFeminin.Should().Be(0);
        dto.Adultes.NombreMasculin.Should().Be(0);

        dto.BranchesScouts.Should().HaveCount(2);
        dto.BranchesScouts.Should().ContainSingle(b =>
            b.Nom == "Oisillons" &&
            b.NombreScouts == 2 &&
            b.NombreFilles == 1 &&
            b.NombreGarcons == 1 &&
            b.Jeunes.Total == 2 &&
            b.Adultes.Total == 0);
        dto.BranchesScouts.Should().ContainSingle(b =>
            b.Nom == "Routier" &&
            b.NombreScouts == 2 &&
            b.NombreFilles == 1 &&
            b.NombreGarcons == 1 &&
            b.Jeunes.Total == 2 &&
            b.Adultes.Total == 0);
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

    [Fact]
    public async Task CreateAsync_New_Group_Inherits_District_Branches()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var districtGroup = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = "Equipe de District Mango Taika"
        };

        db.Groupes.Add(districtGroup);
        db.Branches.AddRange(
            new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Louveteau",
                Description = "8-12 ans",
                AgeMin = 8,
                AgeMax = 12,
                GroupeId = districtGroup.Id
            },
            new Branche
            {
                Id = Guid.NewGuid(),
                Nom = "Eclaireur",
                Description = "12-14 ans",
                AgeMin = 12,
                AgeMax = 14,
                GroupeId = districtGroup.Id
            });
        await db.SaveChangesAsync();

        var inheritance = new DistrictBranchInheritanceService(db);
        var service = new GroupeService(db, new FakeGeocodingService(), inheritance);

        var created = await service.CreateAsync(new GroupeCreateDto
        {
            Nom = "LES PITIKAS",
            Commune = "Cocody"
        });

        created.Nom.Should().Be("LES PITIKAS");
        var inheritedBranches = await db.Branches
            .Where(b => b.GroupeId == created.Id && b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();

        inheritedBranches.Should().HaveCount(2);
        inheritedBranches.Select(b => b.Nom).Should().BeEquivalentTo(["Eclaireur", "Louveteau"]);
        inheritedBranches.Should().OnlyContain(b => b.ChefUniteId == null && b.NomChefUnite == null);
    }
}


