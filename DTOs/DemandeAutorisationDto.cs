using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class DemandeAutorisationDto
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
    public string? MoyensLogistiques { get; set; }
    public string? Budget { get; set; }
    public string? Observations { get; set; }
    public string? TdrContenu { get; set; }
    public StatutDemande Statut { get; set; }
    public string? MotifRejet { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateValidation { get; set; }
    public string? NomDemandeur { get; set; }
    public string? NomValideur { get; set; }
    public string? NomGroupe { get; set; }
    public Guid? GroupeId { get; set; }
    public List<SuiviDemandeDto> Suivis { get; set; } = [];
}

public class DemandeAutorisationCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeActiviteDemande TypeActivite { get; set; }
    public DateTime DateActivite { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Lieu { get; set; }
    public int NombreParticipants { get; set; }
    public string? Objectifs { get; set; }
    public string? MoyensLogistiques { get; set; }
    public string? Budget { get; set; }
    public string? Observations { get; set; }
    public Guid? GroupeId { get; set; }
}

public class SuiviDemandeDto
{
    public StatutDemande AncienStatut { get; set; }
    public StatutDemande NouveauStatut { get; set; }
    public string? Commentaire { get; set; }
    public string? Auteur { get; set; }
    public DateTime Date { get; set; }
}
