namespace MangoTaika.Data.Entities;

public class MembreHistorique
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Description { get; set; }
    public string? Periode { get; set; } // Ex: "2015-2020"
    public CategorieHistorique Categories { get; set; }
    public int Ordre { get; set; }
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<MembreHistoriqueCategorie> CategorieDetails { get; set; } = [];
}

public class MembreHistoriqueCategorie
{
    public Guid Id { get; set; }
    public Guid MembreHistoriqueId { get; set; }
    public MembreHistorique MembreHistorique { get; set; } = null!;
    public CategorieHistorique Categorie { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Description { get; set; }
    public string? Periode { get; set; }
    public int Ordre { get; set; }
}

[Flags]
public enum CategorieHistorique
{
    Aucune = 0,
    AncienCommissaire = 1,
    AncienChefGroupe = 2,
    MembreCAD = 4
}

public static class CategorieHistoriqueExtensions
{
    public static readonly IReadOnlyList<CategorieHistorique> All =
    [
        CategorieHistorique.AncienCommissaire,
        CategorieHistorique.AncienChefGroupe,
        CategorieHistorique.MembreCAD
    ];

    public static IEnumerable<CategorieHistorique> GetSelectedCategories(this CategorieHistorique categories)
        => All.Where(category => category != CategorieHistorique.Aucune && (categories & category) == category);

    public static int GetSortOrder(this CategorieHistorique category)
        => category switch
        {
            CategorieHistorique.AncienCommissaire => 0,
            CategorieHistorique.AncienChefGroupe => 1,
            CategorieHistorique.MembreCAD => 2,
            _ => int.MaxValue
        };

    public static string GetLabel(this CategorieHistorique category)
        => category switch
        {
            CategorieHistorique.AncienCommissaire => "Ancien Commissaire de District",
            CategorieHistorique.AncienChefGroupe => "Ancien Chef de Groupe",
            CategorieHistorique.MembreCAD => "Membre du CAD",
            _ => string.Empty
        };
}
