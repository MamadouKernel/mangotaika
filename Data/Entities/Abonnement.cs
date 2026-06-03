namespace MangoTaika.Data.Entities;

public class ProfilAbonnement
{
    public Guid Id { get; set; }
    public string NomProfil { get; set; } = string.Empty;
    public PeriodiciteAbonnement Periodicite { get; set; } = PeriodiciteAbonnement.Mensuelle;
    public decimal Montant { get; set; }
    public int DelaiHoldJours { get; set; }
    public Guid ComptePaiementMobileId { get; set; }
    public ComptePaiementMobile ComptePaiementMobile { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool EstSupprime { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<AbonnementUtilisateur> Abonnements { get; set; } = [];
}

public class AbonnementUtilisateur
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid ProfilAbonnementId { get; set; }
    public ProfilAbonnement ProfilAbonnement { get; set; } = null!;
    public StatutAbonnement Statut { get; set; } = StatutAbonnement.Actif;
    public DateTime DateDebut { get; set; } = DateTime.UtcNow;
    public DateTime DateEcheance { get; set; }
    public DateTime? DateDernierPaiement { get; set; }
    public bool EstSupprime { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}

public enum PeriodiciteAbonnement
{
    Mensuelle,
    Trimestrielle,
    Semestrielle,
    Annuelle
}

public enum StatutAbonnement
{
    Actif,
    EnHold,
    Inactif
}
