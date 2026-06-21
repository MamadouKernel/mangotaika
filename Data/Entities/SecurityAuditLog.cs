namespace MangoTaika.Data.Entities;

public class SecurityAuditLog
{
    public Guid Id { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public Guid? AuteurId { get; set; }
    public ApplicationUser? Auteur { get; set; }
    public Guid UtilisateurCibleId { get; set; }
    public ApplicationUser UtilisateurCible { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string AncienneValeur { get; set; } = string.Empty;
    public string NouvelleValeur { get; set; } = string.Empty;
    public string? Commentaire { get; set; }
    public string? AdresseIp { get; set; }
}
