namespace MangoTaika.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public string? Lien { get; set; }
    public bool EstLue { get; set; }
    public DateTime DateCreation { get; set; }
}
