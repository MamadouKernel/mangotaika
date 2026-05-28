using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Data.Entities;

public class Ressource
{
    public Guid Id { get; set; }
    [Required]
    [StringLength(160)]
    public string Nom { get; set; } = string.Empty;
    [StringLength(160)]
    public string? Prenom { get; set; }
    [StringLength(40)]
    public string? Telephone { get; set; }
    [StringLength(180)]
    public string? Email { get; set; }
    public TypeRessource Type { get; set; } = TypeRessource.NonPrecise;
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    [StringLength(1000)]
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<ParticipantActivite> ParticipationsActivites { get; set; } = [];
    public ICollection<ParticipationFormationRessource> ParticipationsFormation { get; set; } = [];
}

public enum TypeRessource
{
    Invite,
    Formateur,
    Parrain,
    NonPrecise
}

public class ParticipationFormationRessource
{
    public Guid Id { get; set; }
    public Guid RessourceId { get; set; }
    public Ressource Ressource { get; set; } = null!;
    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
    public StatutParticipationFormationRessource Statut { get; set; } = StatutParticipationFormationRessource.Inscrit;
    public bool EstSupprime { get; set; }
}

public enum StatutParticipationFormationRessource
{
    Inscrit,
    Present,
    Absent,
    Excuse
}
