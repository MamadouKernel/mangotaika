namespace MangoTaika.Data.Entities;

public class Groupe
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string NomNormalise { get; private set; } = string.Empty;
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Adresse { get; set; }
    public string? NomChefGroupe { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid? ResponsableId { get; set; }
    public ApplicationUser? Responsable { get; set; }
    public ICollection<ApplicationUser> Membres { get; set; } = [];
    public ICollection<Branche> Branches { get; set; } = [];
}
