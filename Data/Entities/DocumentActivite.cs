namespace MangoTaika.Data.Entities;

public class DocumentActivite
{
    public Guid Id { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public string CheminFichier { get; set; } = string.Empty;
    public string? TypeDocument { get; set; }
    public DateTime DateUpload { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid ActiviteId { get; set; }
    public Activite Activite { get; set; } = null!;
}
