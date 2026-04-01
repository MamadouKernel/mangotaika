namespace MangoTaika.Data.Entities;

public class Branche
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string NomNormalise { get; private set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? FoulardUrl { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public string? NomChefUnite { get; set; }
    public Guid? ChefUniteId { get; set; }
    public Scout? ChefUnite { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid GroupeId { get; set; }
    public Groupe Groupe { get; set; } = null!;
    public ICollection<Scout> Scouts { get; set; } = [];
    public ICollection<ApplicationUser> Utilisateurs { get; set; } = [];
}
