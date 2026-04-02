namespace MangoTaika.Data.Entities;

public class DemandeAutorisation
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeActiviteDemande TypeActivite { get; set; }
    public DateTime DateActivite { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Lieu { get; set; }
    public int NombreParticipants { get; set; }
    public string? Objectifs { get; set; }
    public string? Responsables { get; set; }
    public string? MoyensLogistiques { get; set; }
    public string? Budget { get; set; }
    public string? Observations { get; set; }

    public string? TdrContenu { get; set; }

    public StatutDemande Statut { get; set; } = StatutDemande.Initialisee;
    public string? MotifRejet { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateValidation { get; set; }

    public Guid DemandeurId { get; set; }
    public ApplicationUser Demandeur { get; set; } = null!;
    public Guid? ValideurId { get; set; }
    public ApplicationUser? Valideur { get; set; }
    public Guid? GroupeId { get; set; }
    public Groupe? Groupe { get; set; }
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }

    public ICollection<SuiviDemande> Suivis { get; set; } = [];
}

public class SuiviDemande
{
    public Guid Id { get; set; }
    public Guid DemandeId { get; set; }
    public DemandeAutorisation Demande { get; set; } = null!;
    public StatutDemande AncienStatut { get; set; }
    public StatutDemande NouveauStatut { get; set; }
    public string? Commentaire { get; set; }
    public string? Auteur { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public enum StatutDemande
{
    Initialisee,
    Soumise,
    EnRevision,
    Validee,
    Rejetee
}

public enum TypeActiviteDemande
{
    Sortie,
    Camp,
    Formation,
    Ceremonie,
    ServiceCommunautaire,
    Autre
}
