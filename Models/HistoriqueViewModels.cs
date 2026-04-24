using System.ComponentModel.DataAnnotations;
using MangoTaika.Data.Entities;

namespace MangoTaika.Models;

public sealed class HistoriqueFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Le nom complet est obligatoire.")]
    [Display(Name = "Nom complet")]
    public string Nom { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    public List<HistoriqueCategorieFormViewModel> Entries { get; set; } = [];

    public IReadOnlyList<CategorieHistorique> GetSelectedCategories()
        => GetNormalizedEntries()
            .Select(entry => entry.Categorie)
            .ToList();

    public List<HistoriqueCategorieFormViewModel> GetNormalizedEntries()
        => Entries
            .Where(entry => CategorieHistoriqueExtensions.All.Contains(entry.Categorie))
            .GroupBy(entry => entry.Categorie)
            .Select(group => group.First())
            .OrderBy(entry => entry.Categorie.GetSortOrder())
            .Select(entry => new HistoriqueCategorieFormViewModel
            {
                Categorie = entry.Categorie,
                Description = NormalizeValue(entry.Description),
                Periode = NormalizeValue(entry.Periode),
                Ordre = entry.Ordre
            })
            .ToList();

    public CategorieHistorique ToFlags()
        => GetNormalizedEntries()
            .Select(entry => entry.Categorie)
            .Aggregate(CategorieHistorique.Aucune, static (current, category) => current | category);

    public static HistoriqueFormViewModel FromEntity(MembreHistorique entity)
        => new()
        {
            Id = entity.Id,
            Nom = entity.Nom,
            PhotoUrl = entity.PhotoUrl,
            Entries = entity.CategorieDetails.Any()
                ? entity.CategorieDetails
                    .OrderBy(detail => detail.Ordre)
                    .ThenBy(detail => detail.Categorie)
                    .Select(detail => new HistoriqueCategorieFormViewModel
                    {
                        Categorie = detail.Categorie,
                        Description = detail.Description,
                        Periode = detail.Periode,
                        Ordre = detail.Ordre
                    })
                    .ToList()
                : entity.Categories
                    .GetSelectedCategories()
                    .Select(category => new HistoriqueCategorieFormViewModel
                    {
                        Categorie = category,
                        Description = entity.Description,
                        Periode = entity.Periode,
                        Ordre = entity.Ordre
                    })
                    .ToList()
        };

    private static string? NormalizeValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HistoriqueCategorieFormViewModel
{
    public CategorieHistorique Categorie { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Période")]
    public string? Periode { get; set; }

    [Display(Name = "Ordre")]
    public int Ordre { get; set; }
}

public sealed class HistoriqueIndexViewModel
{
    public string? Recherche { get; set; }
    public List<CategorieHistorique> Categories { get; set; } = [];
    public List<MembreHistorique> Membres { get; set; } = [];
}
