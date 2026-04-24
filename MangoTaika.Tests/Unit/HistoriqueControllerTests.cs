using FluentAssertions;
using MangoTaika.Controllers;
using MangoTaika.Data.Entities;
using MangoTaika.Models;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class HistoriqueControllerTests
{
    [Fact]
    public async Task Edit_Replaces_Category_Details_Without_Leaving_Deleted_Rows()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var membreId = Guid.NewGuid();

        db.MembresHistoriques.Add(new MembreHistorique
        {
            Id = membreId,
            Nom = "Ancien nom",
            Categories = CategorieHistorique.AncienCommissaire,
            Ordre = 1,
            Periode = "2010-2012",
            Description = "Ancienne description",
            CategorieDetails =
            [
                new MembreHistoriqueCategorie
                {
                    Id = Guid.NewGuid(),
                    MembreHistoriqueId = membreId,
                    Categorie = CategorieHistorique.AncienCommissaire,
                    Periode = "2010-2012",
                    Description = "Ancienne description",
                    Ordre = 1
                }
            ]
        });

        await db.SaveChangesAsync();

        var controller = new HistoriqueController(db, new StubFileUploadService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider())
        };

        controller.ControllerContext.HttpContext.Request.Form =
            new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection());

        var model = new HistoriqueFormViewModel
        {
            Id = membreId,
            Nom = "Nouveau nom",
            Entries =
            [
                new HistoriqueCategorieFormViewModel
                {
                    Categorie = CategorieHistorique.AncienChefGroupe,
                    Periode = "2015-2018",
                    Description = "Chef de groupe historique",
                    Ordre = 2
                },
                new HistoriqueCategorieFormViewModel
                {
                    Categorie = CategorieHistorique.MembreCAD,
                    Periode = "2019-2022",
                    Description = "Membre du CAD historique",
                    Ordre = 3
                }
            ]
        };

        var result = await controller.Edit(membreId, model);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(HistoriqueController.Index));

        db.ChangeTracker.Clear();

        var saved = await db.MembresHistoriques
            .Where(m => m.Id == membreId)
            .Select(m => new
            {
                m.Nom,
                m.Categories,
                m.Ordre,
                m.Periode,
                m.Description,
                Details = m.CategorieDetails
                    .OrderBy(d => d.Ordre)
                    .Select(d => new
                    {
                        d.Categorie,
                        d.Ordre,
                        d.Periode,
                        d.Description
                    })
                    .ToList()
            })
            .SingleAsync();

        saved.Nom.Should().Be("Nouveau nom");
        saved.Categories.Should().Be(CategorieHistorique.AncienChefGroupe | CategorieHistorique.MembreCAD);
        saved.Ordre.Should().Be(2);
        saved.Periode.Should().BeNull();
        saved.Description.Should().BeNull();
        saved.Details.Should().HaveCount(2);
        saved.Details.Select(d => d.Categorie).Should().Equal(
            CategorieHistorique.AncienChefGroupe,
            CategorieHistorique.MembreCAD);
    }

    private sealed class StubFileUploadService : IFileUploadService
    {
        public Task<string> SaveFileAsync(IFormFile file, string subfolder) => Task.FromResult("/uploads/test/file.bin");
        public Task<string> SaveImageAsync(IFormFile file, string subfolder) => Task.FromResult("/uploads/test/image.png");
        public Task<string> SaveMediaAsync(IFormFile file, string subfolder) => Task.FromResult("/uploads/test/media.bin");
        public Task<string> SaveDocumentAsync(IFormFile file, string subfolder, IEnumerable<string>? allowedExtensions = null, long? maxSize = null)
            => Task.FromResult("/uploads/test/document.bin");
        public bool IsValidImage(IFormFile file) => true;
        public bool IsValidMedia(IFormFile file) => true;
        public bool IsValidDocument(IFormFile file, IEnumerable<string>? allowedExtensions = null, long? maxSize = null) => true;
    }

    private sealed class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
