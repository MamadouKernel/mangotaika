namespace MangoTaika.Data.Entities;

public class PortefeuilleUtilisateur
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public decimal Solde { get; set; }
    public string Devise { get; set; } = "XOF";
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<MouvementPortefeuille> Mouvements { get; set; } = [];
}

public class MouvementPortefeuille
{
    public Guid Id { get; set; }
    public Guid PortefeuilleUtilisateurId { get; set; }
    public PortefeuilleUtilisateur PortefeuilleUtilisateur { get; set; } = null!;
    public TypeMouvementPortefeuille Type { get; set; }
    public StatutMouvementPortefeuille Statut { get; set; } = StatutMouvementPortefeuille.EnAttente;
    public decimal Montant { get; set; }
    public string Devise { get; set; } = "XOF";
    public string Libelle { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public Guid? TransfertId { get; set; }
    public string RecuToken { get; set; } = Guid.NewGuid().ToString("N");
    public string? NumeroRecu { get; set; }
    public string? Commentaire { get; set; }
    public decimal? SoldeAvant { get; set; }
    public decimal? SoldeApres { get; set; }
    public string? AdresseIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateValidation { get; set; }
    public Guid? ValideParId { get; set; }
    public ApplicationUser? ValidePar { get; set; }
    public Guid? TransactionFinanciereId { get; set; }
    public TransactionFinanciere? TransactionFinanciere { get; set; }
}

public class ComptePaiementMobile
{
    public Guid Id { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string Operateur { get; set; } = string.Empty;
    public string NumeroMobile { get; set; } = string.Empty;
    public string? NomTitulaire { get; set; }
    public bool EstPrincipal { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public Guid? ModifieParId { get; set; }
    public ApplicationUser? ModifiePar { get; set; }
}

public class DonPublic
{
    public Guid Id { get; set; }
    public string NomDonateur { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public decimal Montant { get; set; }
    public string Devise { get; set; } = "XOF";
    public string? ReferencePaiement { get; set; }
    public string RecuToken { get; set; } = Guid.NewGuid().ToString("N");
    public string? NumeroRecu { get; set; }
    public string? Message { get; set; }
    public StatutDonPublic Statut { get; set; } = StatutDonPublic.Declare;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateTraitement { get; set; }
    public Guid? TraiteParId { get; set; }
    public ApplicationUser? TraitePar { get; set; }
    public Guid? TransactionFinanciereId { get; set; }
    public TransactionFinanciere? TransactionFinanciere { get; set; }
    public string? CommentaireTraitement { get; set; }
}

public enum TypeMouvementPortefeuille
{
    Credit,
    Debit,
    Rechargement,
    Paiement,
    Don
}

public enum StatutMouvementPortefeuille
{
    EnAttente,
    Valide,
    Rejete,
    Annule
}

public enum StatutDonPublic
{
    Declare,
    Confirme,
    Rejete
}
