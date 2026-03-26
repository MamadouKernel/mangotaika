namespace MangoTaika.Data.Entities;

public class HistoriqueTicket
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public StatutTicket AncienStatut { get; set; }
    public StatutTicket NouveauStatut { get; set; }
    public Guid? AuteurId { get; set; }
    public ApplicationUser? Auteur { get; set; }
    public string? Commentaire { get; set; }
    public DateTime DateChangement { get; set; } = DateTime.UtcNow;
}
