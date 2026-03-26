using Microsoft.AspNetCore.Identity;

namespace MangoTaika.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Matricule { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }
    public ICollection<HistoriqueFonction> HistoriqueFonctions { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<NotificationUtilisateur> Notifications { get; set; } = [];
}
