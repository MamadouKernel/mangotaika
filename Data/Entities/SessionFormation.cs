namespace MangoTaika.Data.Entities;

public class SessionFormation
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool EstSelfPaced { get; set; }
    public bool EstPubliee { get; set; } = true;
    public DateTime? DateOuverture { get; set; }
    public DateTime? DateFermeture { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public ICollection<InscriptionFormation> Inscriptions { get; set; } = [];
}
