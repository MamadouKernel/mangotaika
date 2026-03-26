namespace MangoTaika.Data.Entities;

public class NotificationUtilisateur
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Categorie { get; set; } = "Support";
    public string? Lien { get; set; }
    public bool EstLue { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateLecture { get; set; }
}
