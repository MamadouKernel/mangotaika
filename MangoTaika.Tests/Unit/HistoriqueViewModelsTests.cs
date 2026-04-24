using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Models;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class HistoriqueViewModelsTests
{
    [Fact]
    public void ToFlags_Combine_SelectedCategories_WithoutDuplicates()
    {
        var model = new HistoriqueFormViewModel
        {
            Entries =
            [
                new HistoriqueCategorieFormViewModel { Categorie = CategorieHistorique.AncienCommissaire, Ordre = 1 },
                new HistoriqueCategorieFormViewModel { Categorie = CategorieHistorique.MembreCAD, Ordre = 2 },
                new HistoriqueCategorieFormViewModel { Categorie = CategorieHistorique.AncienCommissaire, Ordre = 3 }
            ]
        };

        var flags = model.ToFlags();

        flags.Should().Be(CategorieHistorique.AncienCommissaire | CategorieHistorique.MembreCAD);
    }

    [Fact]
    public void GetSelectedCategories_Returns_AllAssignedCategories_InStableOrder()
    {
        var flags = CategorieHistorique.AncienChefGroupe | CategorieHistorique.MembreCAD;

        var selected = flags.GetSelectedCategories().ToList();

        selected.Should().Equal(
            CategorieHistorique.AncienChefGroupe,
            CategorieHistorique.MembreCAD);
    }

    [Fact]
    public void GetNormalizedEntries_Preserves_CategorySpecific_Data()
    {
        var model = new HistoriqueFormViewModel
        {
            Entries =
            [
                new HistoriqueCategorieFormViewModel
                {
                    Categorie = CategorieHistorique.MembreCAD,
                    Periode = " 2018-2020 ",
                    Description = "  Membre du conseil  ",
                    Ordre = 5
                },
                new HistoriqueCategorieFormViewModel
                {
                    Categorie = CategorieHistorique.AncienChefGroupe,
                    Periode = "2021-2024",
                    Description = "Chef de groupe",
                    Ordre = 2
                }
            ]
        };

        var entries = model.GetNormalizedEntries();

        entries.Should().HaveCount(2);
        entries[0].Categorie.Should().Be(CategorieHistorique.AncienChefGroupe);
        entries[0].Periode.Should().Be("2021-2024");
        entries[1].Categorie.Should().Be(CategorieHistorique.MembreCAD);
        entries[1].Description.Should().Be("Membre du conseil");
    }

    [Fact]
    public void FromEntity_Uses_Category_Details_When_Present()
    {
        var entity = new MembreHistorique
        {
            Id = Guid.NewGuid(),
            Nom = "Ahoua Koffi",
            CategorieDetails =
            [
                new MembreHistoriqueCategorie
                {
                    Id = Guid.NewGuid(),
                    Categorie = CategorieHistorique.AncienCommissaire,
                    Periode = "2010-2014",
                    Description = "Commissaire",
                    Ordre = 1
                },
                new MembreHistoriqueCategorie
                {
                    Id = Guid.NewGuid(),
                    Categorie = CategorieHistorique.MembreCAD,
                    Periode = "2015-2017",
                    Description = "CAD",
                    Ordre = 2
                }
            ]
        };

        var model = HistoriqueFormViewModel.FromEntity(entity);

        model.Entries.Should().HaveCount(2);
        model.Entries.Select(entry => entry.Categorie).Should().Equal(
            CategorieHistorique.AncienCommissaire,
            CategorieHistorique.MembreCAD);
    }
}
