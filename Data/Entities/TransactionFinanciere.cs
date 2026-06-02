namespace MangoTaika.Data.Entities;

public class TransactionFinanciere
{
    public Guid Id { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public TypeTransaction Type { get; set; }
    public CategorieFinance Categorie { get; set; }
    public StatutTransactionFinanciere Statut { get; set; } = StatutTransactionFinanciere.Validee;
    public DateTime DateTransaction { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public string? Commentaire { get; set; }
    public bool EstSupprime { get; set; } = false;

    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public Guid? ActiviteId { get; set; }
    public Activite? Activite { get; set; }
    public Guid? ProjetAGRId { get; set; }
    public ProjetAGR? ProjetAGR { get; set; }
    public Guid? ScoutId { get; set; }
    public Scout? Scout { get; set; }
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
}

public enum TypeTransaction { Recette, Depense }

public enum StatutTransactionFinanciere
{
    Declaree,
    Validee,
    Rejetee
}

public enum CategorieFinance
{
    Cotisation,
    Subvention,
    Don,
    Activite,
    Materiel,
    Transport,
    Alimentation,
    AGR,
    Autre
}
