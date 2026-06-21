namespace MangoTaika.Data.Entities;

public class DemandeRapprochementCompte
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? ScoutId { get; set; }
    public Scout? Scout { get; set; }
    public string RoleDemande { get; set; } = string.Empty;
    public StatutDemandeRapprochement Statut { get; set; } = StatutDemandeRapprochement.EnAttente;
    public string Motif { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateTraitement { get; set; }
    public Guid? TraiteParId { get; set; }
    public ApplicationUser? TraitePar { get; set; }
}

public enum StatutDemandeRapprochement
{
    EnAttente,
    Approuvee,
    Rejetee
}
