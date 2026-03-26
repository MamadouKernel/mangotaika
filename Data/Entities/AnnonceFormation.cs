namespace MangoTaika.Data.Entities;

public class AnnonceFormation
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public bool EstPubliee { get; set; } = true;
    public DateTime DatePublication { get; set; } = DateTime.UtcNow;

    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public Guid? AuteurId { get; set; }
    public ApplicationUser? Auteur { get; set; }
}
