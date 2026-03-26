namespace MangoTaika.Data.Entities;

public class MembreHistorique
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Description { get; set; }
    public string? Periode { get; set; } // Ex: "2015-2020"
    public CategorieHistorique Categorie { get; set; }
    public int Ordre { get; set; }
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}

public enum CategorieHistorique
{
    AncienCommissaire,
    AncienChefGroupe,
    MembreCAD
}
