namespace MangoTaika.Data.Entities;

public class Actualite
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Resume { get; set; }
    public DateTime DatePublication { get; set; } = DateTime.UtcNow;
    public bool EstPublie { get; set; } = false;
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
}
