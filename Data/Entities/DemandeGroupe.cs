namespace MangoTaika.Data.Entities;

public class DemandeGroupe
{
    public Guid Id { get; set; }
    public string NomGroupe { get; set; } = string.Empty;
    public string Commune { get; set; } = string.Empty;
    public string Quartier { get; set; } = string.Empty;
    public string NomResponsable { get; set; } = string.Empty;
    public string TelephoneResponsable { get; set; } = string.Empty;
    public string? EmailResponsable { get; set; }
    public string? Motivation { get; set; }
    public int NombreMembresPrevus { get; set; }
    public StatutDemandeGroupe Statut { get; set; } = StatutDemandeGroupe.EnAttente;
    public string? MotifRejet { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateTraitement { get; set; }
    public Guid? TraiteParId { get; set; }
    public ApplicationUser? TraitePar { get; set; }
}

public enum StatutDemandeGroupe
{
    EnAttente,
    Approuvee,
    Rejetee
}
