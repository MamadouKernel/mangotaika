namespace MangoTaika.Data.Entities;

public class CommentaireActivite
{
    public Guid Id { get; set; }
    public Guid ActiviteId { get; set; }
    public Activite Activite { get; set; } = null!;
    public Guid AuteurId { get; set; }
    public ApplicationUser Auteur { get; set; } = null!;
    public string Contenu { get; set; } = string.Empty;
    public string? TypeAction { get; set; } // Création, Soumission, Validation, Rejet, Commentaire, Clôture
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
