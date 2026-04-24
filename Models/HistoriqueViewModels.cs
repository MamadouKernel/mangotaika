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

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Période")]
    public string? Periode { get; set; }

    [Display(Name = "Ordre")]
    public int Ordre { get; set; }

    [Display(Name = "Catégories")]
    public List<CategorieHistorique> Categories { get; set; } = [];

    public CategorieHistorique ToFlags()
        => Categories
            .Distinct()
            .Aggregate(CategorieHistorique.Aucune, static (current, category) => current | category);

    public static HistoriqueFormViewModel FromEntity(MembreHistorique entity)
        => new()
        {
            Id = entity.Id,
            Nom = entity.Nom,
            PhotoUrl = entity.PhotoUrl,
            Description = entity.Description,
            Periode = entity.Periode,
            Ordre = entity.Ordre,
            Categories = entity.Categories.GetSelectedCategories().ToList()
        };
}

public sealed class HistoriqueIndexViewModel
{
    public string? Recherche { get; set; }
    public List<CategorieHistorique> Categories { get; set; } = [];
    public List<MembreHistorique> Membres { get; set; } = [];
}
