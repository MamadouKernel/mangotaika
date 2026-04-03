namespace MangoTaika.Data.Entities;

public class Activite
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeActivite Type { get; set; } = TypeActivite.Autre;
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Lieu { get; set; }
    public decimal? BudgetPrevisionnel { get; set; }
    public string? NomResponsable { get; set; }
    public StatutActivite Statut { get; set; } = StatutActivite.Brouillon;
    public string? MotifRejet { get; set; }
    public DateTime? DateCloturePointage { get; set; }
    public bool EstSupprime { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public ICollection<DocumentActivite> Documents { get; set; } = [];
    public ICollection<ParticipantActivite> Participants { get; set; } = [];
    public ICollection<CommentaireActivite> Commentaires { get; set; } = [];
}

public enum TypeActivite
{
    Camp,
    Sortie,
    Reunion,
    Formation,
    Ceremonie,
    Autre
}

public enum StatutActivite
{
    Brouillon,
    Soumise,
    Validee,
    Rejetee,
    EnCours,
    Terminee,
    Archivee
}

