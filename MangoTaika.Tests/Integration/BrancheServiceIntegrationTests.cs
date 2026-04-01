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
        var inheritance = new DistrictBranchInheritanceService(db);

        var groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Riviera" };
        var chef = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583762X",
            Nom = "Chef",
            Prenom = "Alpha",
            DateNaissance = new DateTime(2010, 1, 1),
            GroupeId = groupe.Id,
            Fonction = "CHEF D'UNITE (CU)"
        };

        db.Groupes.Add(groupe);
        db.Scouts.Add(chef);
        await db.SaveChangesAsync();

        var service = new BrancheService(db, inheritance);

        var created = await service.CreateAsync(new BrancheCreateDto
        {
            Nom = " Louveteaux ",
            Description = " Branche des 8-12 ans ",
            LogoUrl = "/uploads/branches/louveteaux.png",
            AgeMin = 8,
            AgeMax = 12,
            GroupeId = groupe.Id,
            ChefUniteId = chef.Id
        });

        var branche = await db.Branches.Include(b => b.ChefUnite).SingleAsync();

        created.Nom.Should().Be("Louveteaux");
        created.NomChefUnite.Should().Be("Alpha Chef");
        created.LogoUrl.Should().Be("/uploads/branches/louveteaux.png");
        branche.Nom.Should().Be("Louveteaux");
        branche.Description.Should().Be("Branche des 8-12 ans");
        branche.LogoUrl.Should().Be("/uploads/branches/louveteaux.png");
        branche.GroupeId.Should().Be(groupe.Id);
        branche.ChefUniteId.Should().Be(chef.Id);
        branche.NomChefUnite.Should().Be("Alpha Chef");
    }

    [Fact]
    public async Task CreateAsync_Rejects_Duplicate_Name_In_Same_Groupe()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var inheritance = new DistrictBranchInheritanceService(db);

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

        var service = new BrancheService(db, inheritance);

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

    [Fact]
    public async Task CreateAsync_Rejects_Duplicate_Name_When_Accents_And_Apostrophes_Differ()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var inheritance = new DistrictBranchInheritanceService(db);

        var groupe = new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Riviera" };
        var chef = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583768X",
            Nom = "Chef",
            Prenom = "Gamma",
            DateNaissance = new DateTime(2010, 1, 1),
            GroupeId = groupe.Id
        };

        db.Groupes.Add(groupe);
        db.Scouts.Add(chef);
        db.Branches.Add(new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Aines d'Abidjan",
            GroupeId = groupe.Id,
            ChefUniteId = chef.Id,
            NomChefUnite = "Gamma Chef"
        });
        await db.SaveChangesAsync();

        var service = new BrancheService(db, inheritance);

        Func<Task> act = () => service.CreateAsync(new BrancheCreateDto
        {
            Nom = "Ainés d Abidjan",
            GroupeId = groupe.Id,
            ChefUniteId = chef.Id
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Une branche avec ce nom existe deja dans ce groupe.*");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_District_Wide_Branche_Details_By_Groupe()
    {
        await using var db = TestDbContextFactory.CreateDbContext();

        var groupeA = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = "LES AHES",
            LogoUrl = "/uploads/groupes/ahes.png"
        };
        var groupeB = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = "HAPUU RERU",
            LogoUrl = "/uploads/groupes/hapuu.png"
        };

        var chefA = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583901A",
            Nom = "Edgar",
            Prenom = "Yann",
            PhotoUrl = "/uploads/scouts/yann.png",
            DateNaissance = DateTime.UtcNow.AddYears(-24),
            GroupeId = groupeA.Id
        };

        var chefB = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583902B",
            Nom = "Luc",
            Prenom = "Kouassi",
            DateNaissance = DateTime.UtcNow.AddYears(-26),
            GroupeId = groupeB.Id
        };

        var brancheA = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Eclaireur",
            GroupeId = groupeA.Id,
            ChefUniteId = chefA.Id,
            NomChefUnite = "Yann Edgar",
            LogoUrl = "/uploads/branches/eclaireur.png"
        };

        var brancheB = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Eclaireur",
            GroupeId = groupeB.Id,
            ChefUniteId = chefB.Id,
            NomChefUnite = "Kouassi Luc"
        };

        var brancheOther = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Louveteau",
            GroupeId = groupeA.Id
        };

        db.Groupes.AddRange(groupeA, groupeB);
        db.Scouts.AddRange(chefA, chefB);
        db.Branches.AddRange(brancheA, brancheB, brancheOther);
        db.Scouts.AddRange(
            CreateScout(groupeA.Id, brancheA.Id, "0583903C", "Aka", "Awa", DateTime.UtcNow.AddYears(-13), "Feminin", "CP"),
            CreateScout(groupeA.Id, brancheA.Id, "0583904D", "Yao", "Koffi", DateTime.UtcNow.AddYears(-14), "Masculin", null),
            CreateScout(groupeA.Id, brancheA.Id, "0583905E", "Kone", "Mariam", DateTime.UtcNow.AddYears(-21), "Feminin", "Animatrice"),
            CreateScout(groupeB.Id, brancheB.Id, "0583906F", "Kouame", "Jean", DateTime.UtcNow.AddYears(-12), "Masculin", "CP"),
            CreateScout(groupeB.Id, brancheB.Id, "0583907G", "Soro", "Lea", DateTime.UtcNow.AddYears(-20), "Feminin", "Cheffe"),
            CreateScout(groupeA.Id, brancheOther.Id, "0583908H", "Autre", "Branche", DateTime.UtcNow.AddYears(-10), "Feminin", null));
        await db.SaveChangesAsync();

        var inheritance = new DistrictBranchInheritanceService(db);
        var service = new BrancheService(db, inheritance);

        var dto = await service.GetByIdAsync(brancheA.Id);

        dto.Should().NotBeNull();
        dto!.ResponsablePhotoUrl.Should().Be("/uploads/scouts/yann.png");
        dto.NombreScouts.Should().Be(5);
        dto.NombreFilles.Should().Be(3);
        dto.NombreGarcons.Should().Be(2);
        dto.Jeunes.Total.Should().Be(3);
        dto.Adultes.Total.Should().Be(2);
        dto.TotauxParGroupes.Should().HaveCount(2);
        dto.TotauxParGroupes.Should().ContainSingle(g =>
            g.NomGroupe == "LES AHES" &&
            g.NombreScouts == 3 &&
            g.NombreFilles == 2 &&
            g.NombreGarcons == 1 &&
            g.NombreJeunes == 2 &&
            g.NombreAdultes == 1 &&
            g.Jeunes.NombreFeminin == 1 &&
            g.Jeunes.NombreMasculin == 1 &&
            g.Adultes.NombreFeminin == 1 &&
            g.Adultes.NombreMasculin == 0);
        dto.TotauxParGroupes.Should().ContainSingle(g =>
            g.NomGroupe == "HAPUU RERU" &&
            g.NombreScouts == 2 &&
            g.NombreFilles == 1 &&
            g.NombreGarcons == 1 &&
            g.NombreJeunes == 1 &&
            g.NombreAdultes == 1 &&
            g.Jeunes.NombreFeminin == 0 &&
            g.Jeunes.NombreMasculin == 1 &&
            g.Adultes.NombreFeminin == 1 &&
            g.Adultes.NombreMasculin == 0);
        dto.Membres.Should().HaveCount(5);
        dto.Membres.Should().Contain(m => m.Matricule == "0583905E" && m.Groupe == "LES AHES" && m.Fonction == "Animatrice");
        dto.Membres.Should().NotContain(m => m.Matricule == "0583908H");
    }

    private static Scout CreateScout(Guid groupeId, Guid brancheId, string matricule, string nom, string prenom, DateTime dateNaissance, string sexe, string? fonction)
    {
        return new Scout
        {
            Id = Guid.NewGuid(),
            GroupeId = groupeId,
            BrancheId = brancheId,
            Matricule = matricule,
            Nom = nom,
            Prenom = prenom,
            DateNaissance = dateNaissance,
            Sexe = sexe,
            Fonction = fonction,
            IsActive = true
        };
    }

    [Fact]
    public async Task CreateAsync_District_Branch_Is_Propagated_To_All_Other_Groups()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var inheritance = new DistrictBranchInheritanceService(db);

        var districtGroup = new Groupe { Id = Guid.NewGuid(), Nom = "Equipe de District Mango Taika" };
        var groupeA = new Groupe { Id = Guid.NewGuid(), Nom = "LES AIHES" };
        var groupeB = new Groupe { Id = Guid.NewGuid(), Nom = "HAPUU RERU" };
        var chefDistrict = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = "0583998X",
            Nom = "Edgar",
            Prenom = "Yann",
            DateNaissance = new DateTime(2000, 1, 1),
            GroupeId = districtGroup.Id,
            Fonction = "ASSISTANT COMMISSAIRE DE DISTRICT (ACD)"
        };

        db.Groupes.AddRange(districtGroup, groupeA, groupeB);
        db.Scouts.Add(chefDistrict);
        await db.SaveChangesAsync();

        var service = new BrancheService(db, inheritance);

        var created = await service.CreateAsync(new BrancheCreateDto
        {
            Nom = "Eclaireur",
            Description = "12-14 ans",
            AgeMin = 12,
            AgeMax = 14,
            GroupeId = districtGroup.Id,
            ChefUniteId = chefDistrict.Id
        });

        created.Nom.Should().Be("Eclaireur");

        var allBranches = await db.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.GroupeId)
            .ToListAsync();

        allBranches.Should().HaveCount(3);
        allBranches.Should().ContainSingle(b => b.GroupeId == districtGroup.Id && b.ChefUniteId == chefDistrict.Id);
        allBranches.Should().ContainSingle(b => b.GroupeId == groupeA.Id && b.ChefUniteId == null && b.NomChefUnite == null);
        allBranches.Should().ContainSingle(b => b.GroupeId == groupeB.Id && b.ChefUniteId == null && b.NomChefUnite == null);
    }

    [Fact]
    public async Task EnsureInheritedBranchesAsync_Backfills_Existing_Groups_From_District_Group()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var districtGroup = new Groupe { Id = Guid.NewGuid(), Nom = "Equipe de District Mango Taika" };
        var groupeA = new Groupe { Id = Guid.NewGuid(), Nom = "LES AIHES" };
        var groupeB = new Groupe { Id = Guid.NewGuid(), Nom = "HAPUU RERU" };

        db.Groupes.AddRange(districtGroup, groupeA, groupeB);
        db.Branches.Add(new Branche
        {
            Id = Guid.NewGuid(),
            Nom = "Routier",
            Description = "17-25 ans",
            AgeMin = 17,
            AgeMax = 25,
            GroupeId = districtGroup.Id
        });
        await db.SaveChangesAsync();

        var inheritance = new DistrictBranchInheritanceService(db);

        await inheritance.EnsureInheritedBranchesAsync();

        var propagatedBranches = await db.Branches
            .Where(b => b.IsActive && b.Nom == "Routier")
            .ToListAsync();

        propagatedBranches.Should().HaveCount(3);
        propagatedBranches.Should().Contain(b => b.GroupeId == groupeA.Id);
        propagatedBranches.Should().Contain(b => b.GroupeId == groupeB.Id);
    }
}

