namespace MangoTaika.Data.Entities;

public class CertificationFormation
{
    public Guid Id { get; set; }
    public TypeCertificationFormation Type { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime DateEmission { get; set; } = DateTime.UtcNow;
    public int ScoreFinal { get; set; }
    public string Mention { get; set; } = string.Empty;

    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;
    public Guid? InscriptionFormationId { get; set; }
    public InscriptionFormation? InscriptionFormation { get; set; }
}

public enum TypeCertificationFormation
{
    Badge = 0,
    Attestation = 1,
    Certificat = 2
}
