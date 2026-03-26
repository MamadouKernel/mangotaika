namespace MangoTaika.Data.Entities;

public class HistoriqueFonction
{
    public Guid Id { get; set; }
    public string Fonction { get; set; } = string.Empty;
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Commentaire { get; set; }

    // Navigation
    public Guid? ScoutId { get; set; }
    public Scout? Scout { get; set; }
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
}
