namespace MangoTaika.Data.Entities;

public class Scout
{
    public Guid Id { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public DateTime DateNaissance { get; set; }
    public string? LieuNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? RegionScoute { get; set; }
    public string? District { get; set; }
    public string? NumeroCarte { get; set; }
    public string? Fonction { get; set; }
    public string? FonctionVieActive { get; set; }
    public string? NiveauFormationScoute { get; set; }
    public string? ContactUrgenceNom { get; set; }
    public string? ContactUrgenceRelation { get; set; }
    public string? ContactUrgenceTelephone { get; set; }
    public string? StatutASCCI { get; set; }
    public bool AssuranceAnnuelle { get; set; } = false;
    public string? AdresseGeographique { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }
    public ICollection<Parent> Parents { get; set; } = [];
    public ICollection<Competence> Competences { get; set; } = [];
    public ICollection<HistoriqueFonction> HistoriqueFonctions { get; set; } = [];
    public ICollection<TransactionFinanciere> Cotisations { get; set; } = [];
    public ICollection<SuiviAcademique> SuivisAcademiques { get; set; } = [];
    public ICollection<EtapeParcoursScout> EtapesParcours { get; set; } = [];
}
