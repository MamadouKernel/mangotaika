namespace MangoTaika.Data.Entities;

public class ArticleBoutique
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Categorie { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Prix { get; set; }
    public string Devise { get; set; } = "XOF";
    public int StockDisponible { get; set; }
    public bool EstPublie { get; set; } = true;
    public bool EstSupprime { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateModification { get; set; }
}

public class CommandeBoutique
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public ApplicationUser? Client { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string TelephoneClient { get; set; } = string.Empty;
    public string? EmailClient { get; set; }
    public StatutCommandeBoutique Statut { get; set; } = StatutCommandeBoutique.EnAttente;
    public decimal Total { get; set; }
    public string Devise { get; set; } = "XOF";
    public string? ReferencePaiement { get; set; }
    public ModePaiementCommandeBoutique ModePaiement { get; set; } = ModePaiementCommandeBoutique.MobileMoney;
    public string? NumeroRecu { get; set; }
    public string RecuToken { get; set; } = Guid.NewGuid().ToString("N");
    public string? CommentaireTraitement { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateTraitement { get; set; }
    public Guid? TraiteParId { get; set; }
    public ApplicationUser? TraitePar { get; set; }
    public ICollection<LigneCommandeBoutique> Lignes { get; set; } = [];
}

public class LigneCommandeBoutique
{
    public Guid Id { get; set; }
    public Guid CommandeBoutiqueId { get; set; }
    public CommandeBoutique CommandeBoutique { get; set; } = null!;
    public Guid ArticleBoutiqueId { get; set; }
    public ArticleBoutique ArticleBoutique { get; set; } = null!;
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
}

public enum StatutCommandeBoutique
{
    EnAttente,
    Payee,
    Livree,
    Annulee
}

public enum ModePaiementCommandeBoutique
{
    MobileMoney,
    PaiementLivraison,
    Portefeuille
}
