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
            Categories =
            [
                CategorieHistorique.AncienCommissaire,
                CategorieHistorique.MembreCAD,
                CategorieHistorique.AncienCommissaire
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
}
