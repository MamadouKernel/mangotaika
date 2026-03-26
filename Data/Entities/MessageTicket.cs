namespace MangoTaika.Data.Entities;

public class MessageTicket
{
    public Guid Id { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public bool EstNoteInterne { get; set; }
    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public Guid AuteurId { get; set; }
    public ApplicationUser Auteur { get; set; } = null!;
}
