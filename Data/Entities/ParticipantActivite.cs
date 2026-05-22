namespace MangoTaika.Data.Entities;

public class ParticipantActivite
{
    public Guid Id { get; set; }
    public Guid ActiviteId { get; set; }
    public Activite Activite { get; set; } = null!;
    public Guid? ScoutId { get; set; }
    public Scout? Scout { get; set; }
    public Guid? RessourceId { get; set; }
    public Ressource? Ressource { get; set; }
    public StatutPresence Presence { get; set; } = StatutPresence.Inscrit;
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
}

public enum StatutPresence
{
    Inscrit,
    Present,
    Absent,
    Excuse
}
