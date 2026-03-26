namespace MangoTaika.Data.Entities;

public class MotCommissaire
{
    public Guid Id { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public int Annee { get; set; }
    public bool EstActif { get; set; } = true;
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
