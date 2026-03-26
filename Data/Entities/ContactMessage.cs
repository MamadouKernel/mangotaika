namespace MangoTaika.Data.Entities;

public class ContactMessage
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Sujet { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Contact"; // Contact ou Avis
    public bool EstLu { get; set; } = false;
    public bool EstSupprime { get; set; } = false;
    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;
}
