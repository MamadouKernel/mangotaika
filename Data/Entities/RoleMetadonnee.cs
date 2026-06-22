namespace MangoTaika.Data.Entities;

public class RoleMetadonnee
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Visibilite { get; set; }
    public int Hierarchie { get; set; } = 50;
    public bool EstSysteme { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
