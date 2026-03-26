namespace MangoTaika.Data.Entities;

public class Galerie
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CheminMedia { get; set; } = string.Empty;
    public string TypeMedia { get; set; } = "image"; // image, video
    public DateTime DateUpload { get; set; } = DateTime.UtcNow;
    public bool EstPublie { get; set; } = false;
    public bool EstSupprime { get; set; } = false;
}
