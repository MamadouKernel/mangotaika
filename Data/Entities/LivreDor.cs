namespace MangoTaika.Data.Entities;

public class LivreDor
{
    public Guid Id { get; set; }
    public string NomAuteur { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool EstValide { get; set; } = false;
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateValidation { get; set; }
}
