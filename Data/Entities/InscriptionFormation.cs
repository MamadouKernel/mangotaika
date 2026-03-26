namespace MangoTaika.Data.Entities;

public class InscriptionFormation
{
    public Guid Id { get; set; }
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
    public StatutInscription Statut { get; set; } = StatutInscription.EnCours;
    public DateTime? DateTerminee { get; set; }
    public int ProgressionPourcent { get; set; } = 0;

    // Navigation
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;
    public Guid? SessionFormationId { get; set; }
    public SessionFormation? SessionFormation { get; set; }
    public ICollection<CertificationFormation> Certifications { get; set; } = [];
}

public enum StatutInscription { EnCours, Terminee, Abandonnee }
