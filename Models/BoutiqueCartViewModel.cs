using MangoTaika.Data.Entities;

namespace MangoTaika.Models;

public sealed class BoutiqueCartItem
{
    public Guid ArticleId { get; set; }
    public int Quantite { get; set; }
}

public sealed class BoutiqueCartLineViewModel
{
    public ArticleBoutique Article { get; set; } = null!;
    public int Quantite { get; set; }
    public int QuantiteMax { get; set; }
    public decimal Total => Article.Prix * Quantite;
}

public sealed class BoutiqueCartViewModel
{
    public List<BoutiqueCartLineViewModel> Lignes { get; set; } = [];
    public decimal Total => Lignes.Sum(l => l.Total);
    public int NombreArticles => Lignes.Sum(l => l.Quantite);
    public string Devise => Lignes.FirstOrDefault()?.Article.Devise ?? "XOF";
}
