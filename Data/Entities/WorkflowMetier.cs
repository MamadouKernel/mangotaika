using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Data.Entities;

public enum StatutWorkflowDocument
{
    Brouillon,
    Soumis,
    AReviser,
    Valide
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
}

public class RapportActivite
{
    public Guid Id { get; set; }
    public Guid ActiviteId { get; set; }
    public Activite Activite { get; set; } = null!;
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
}

