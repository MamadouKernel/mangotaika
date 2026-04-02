using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Data.Entities;

public enum StatutWorkflowDocument
{
    Brouillon,
    Soumis,
    AReviser,
    Valide,
    Rejete
}

public enum StatutInscriptionAnnuelle
{
    Enregistree,
    Validee,
    Suspendue
}

public class InscriptionAnnuelleScout
{
    public Guid Id { get; set; }
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }
    [StringLength(180)]
    public string? FonctionSnapshot { get; set; }
    [Range(2000, 2100)]
    public int AnneeReference { get; set; }
    [Required]
    [StringLength(32)]
    public string LibelleAnnee { get; set; } = string.Empty;
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
    public DateTime? DateValidation { get; set; }
    public StatutInscriptionAnnuelle Statut { get; set; } = StatutInscriptionAnnuelle.Enregistree;
    public bool InscriptionParoissialeValidee { get; set; }
    public bool CotisationNationaleAjour { get; set; }
    [StringLength(1000)]
    public string? Observations { get; set; }
    public Guid? ValideParId { get; set; }
    public ApplicationUser? ValidePar { get; set; }
}

public class ProgrammeAnnuel
{
    public Guid Id { get; set; }
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    [Range(2000, 2100)]
    public int AnneeReference { get; set; }
    [Required]
    [StringLength(180)]
    public string Titre { get; set; } = string.Empty;
    [Required]
    [StringLength(4000)]
    public string Objectifs { get; set; } = string.Empty;
    [Required]
    [StringLength(6000)]
    public string CalendrierSynthese { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Observations { get; set; }
    public StatutWorkflowDocument Statut { get; set; } = StatutWorkflowDocument.Brouillon;
    [StringLength(1500)]
    public string? CommentaireValidation { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateSoumission { get; set; }
    public DateTime? DateValidation { get; set; }
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public Guid? ValideurId { get; set; }
    public ApplicationUser? Valideur { get; set; }
    public ICollection<ProgrammeAnnuelActivite> Activites { get; set; } = [];
}

public class ProgrammeAnnuelActivite
{
    public Guid Id { get; set; }
    public Guid ProgrammeAnnuelId { get; set; }
    public ProgrammeAnnuel ProgrammeAnnuel { get; set; } = null!;
    [Required]
    [StringLength(180)]
    public string NomActivite { get; set; } = string.Empty;
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }
    [StringLength(180)]
    public string? Cible { get; set; }
    [Required]
    [StringLength(1500)]
    public string Objectif { get; set; } = string.Empty;
    [StringLength(250)]
    public string? Lieu { get; set; }
    public DateTime DateActivite { get; set; }
    [Required]
    [StringLength(180)]
    public string Responsable { get; set; } = string.Empty;
    [Required]
    [StringLength(2500)]
    public string Description { get; set; } = string.Empty;
    [Range(0, 1000000000)]
    public decimal? MontantParticipation { get; set; }
    [Range(1, 999)]
    public int OrdreAffichage { get; set; } = 1;
}

public class RapportActivite
{
    public Guid Id { get; set; }
    public Guid ActiviteId { get; set; }
    public Activite Activite { get; set; } = null!;
    public DateTime DateRealisation { get; set; } = DateTime.UtcNow;
    [Range(0, 50000)]
    public int NombreParticipants { get; set; }
    [Required]
    [StringLength(2000)]
    public string ResumeExecutif { get; set; } = string.Empty;
    [Required]
    [StringLength(4000)]
    public string ResultatsObtenus { get; set; } = string.Empty;
    [Required]
    [StringLength(4000)]
    public string DifficultesRencontrees { get; set; } = string.Empty;
    [Required]
    [StringLength(4000)]
    public string Recommandations { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? ObservationsComplementaires { get; set; }
    public StatutWorkflowDocument Statut { get; set; } = StatutWorkflowDocument.Brouillon;
    [StringLength(1500)]
    public string? CommentaireValidation { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateSoumission { get; set; }
    public DateTime? DateValidation { get; set; }
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public Guid? ValideurId { get; set; }
    public ApplicationUser? Valideur { get; set; }
    public ICollection<RapportActivitePieceJointe> PiecesJointes { get; set; } = [];
}

public class RapportActivitePieceJointe
{
    public Guid Id { get; set; }
    public Guid RapportActiviteId { get; set; }
    public RapportActivite RapportActivite { get; set; } = null!;
    [Required]
    [StringLength(260)]
    public string NomFichier { get; set; } = string.Empty;
    [Required]
    [StringLength(500)]
    public string UrlFichier { get; set; } = string.Empty;
    [StringLength(150)]
    public string? TypeMime { get; set; }
    public DateTime DateAjout { get; set; } = DateTime.UtcNow;
}

public class PropositionMaitriseAnnuelle
{
    public Guid Id { get; set; }
    public Guid GroupeId { get; set; }
    public Groupe Groupe { get; set; } = null!;
    [Range(2000, 2100)]
    public int AnneeReference { get; set; }
    [Required]
    [StringLength(180)]
    public string Titre { get; set; } = string.Empty;
    [Required]
    [StringLength(5000)]
    public string CompositionProposee { get; set; } = string.Empty;
    [Required]
    [StringLength(3000)]
    public string ObjectifsPedagogiques { get; set; } = string.Empty;
    [StringLength(3000)]
    public string? BesoinsFormation { get; set; }
    [StringLength(2000)]
    public string? Observations { get; set; }
    public StatutWorkflowDocument Statut { get; set; } = StatutWorkflowDocument.Brouillon;
    [StringLength(1500)]
    public string? CommentaireValidation { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateSoumission { get; set; }
    public DateTime? DateValidation { get; set; }
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public Guid? ValideurId { get; set; }
    public ApplicationUser? Valideur { get; set; }
    public ICollection<PropositionMaitriseMembre> Membres { get; set; } = [];
}

public class PropositionMaitriseMembre
{
    public Guid Id { get; set; }
    public Guid PropositionMaitriseAnnuelleId { get; set; }
    public PropositionMaitriseAnnuelle PropositionMaitriseAnnuelle { get; set; } = null!;
    [Required]
    [StringLength(180)]
    public string NomChef { get; set; } = string.Empty;
    [Required]
    [StringLength(180)]
    public string Fonction { get; set; } = string.Empty;
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }
    [Required]
    [StringLength(180)]
    public string Contact { get; set; } = string.Empty;
    [StringLength(180)]
    public string? NiveauFormation { get; set; }
    [Range(1, 999)]
    public int OrdreAffichage { get; set; } = 1;
}
