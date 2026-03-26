namespace MangoTaika.Data.Entities;

public class Formation
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public NiveauFormation Niveau { get; set; } = NiveauFormation.Debutant;
    public StatutFormation Statut { get; set; } = StatutFormation.Brouillon;
    public int DureeEstimeeHeures { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DatePublication { get; set; }
    public bool DelivreBadge { get; set; } = true;
    public bool DelivreAttestation { get; set; } = true;
    public bool DelivreCertificat { get; set; }
    public bool DelivranceConfiguree { get; set; } = true;

    // Branche cible (optionnel)
    public Guid? BrancheCibleId { get; set; }
    public Branche? BrancheCible { get; set; }

    // Compétence liée (optionnel) — auto-ajoutée à la fin
    public Guid? CompetenceLieeId { get; set; }

    // Auteur
    public Guid AuteurId { get; set; }
    public ApplicationUser Auteur { get; set; } = null!;

    // Navigation
    public ICollection<ModuleFormation> Modules { get; set; } = [];
    public ICollection<InscriptionFormation> Inscriptions { get; set; } = [];
    public ICollection<SessionFormation> Sessions { get; set; } = [];
    public ICollection<AnnonceFormation> Annonces { get; set; } = [];
    public ICollection<CertificationFormation> Certifications { get; set; } = [];
    public ICollection<DiscussionFormation> Discussions { get; set; } = [];
    public ICollection<JalonFormation> Jalons { get; set; } = [];
    public ICollection<FormationPrerequis> Prerequis { get; set; } = [];
    public ICollection<FormationPrerequis> FormationDebloqueesParCeCours { get; set; } = [];
}

public enum NiveauFormation { Debutant, Intermediaire, Avance }
public enum StatutFormation { Brouillon, Publiee, Archivee }
