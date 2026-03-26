namespace MangoTaika.Data.Entities;

public class ProjetAGR
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public StatutProjetAGR Statut { get; set; } = StatutProjetAGR.Planifie;
    public decimal BudgetInitial { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Responsable { get; set; }
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public ICollection<TransactionFinanciere> Transactions { get; set; } = [];
}

public enum StatutProjetAGR { Planifie, EnCours, Termine, Annule }
