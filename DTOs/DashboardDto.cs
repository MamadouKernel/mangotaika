using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class DashboardDto
{
    public string RoleActif { get; set; } = string.Empty;
    public string TitreBienvenue { get; set; } = string.Empty;
    public string SousTitreBienvenue { get; set; } = string.Empty;
    public string? MessageInfo { get; set; }
    public int TotalScouts { get; set; }
    public int TotalGroupes { get; set; }
    public int TotalBranches { get; set; }
    public int ActivitesEnCours { get; set; }
    public int TicketsOuverts { get; set; }
    public int MesTicketsAssignes { get; set; }
    public int TicketsEscalades { get; set; }
    public int TicketsEnRetardSla { get; set; }
    public int NotificationsNonLues { get; set; }
    public double TauxRespectSla { get; set; }
    public int MessagesNonLus { get; set; }
    public int DemandesAutorisationEnAttente { get; set; }
    public int DemandesGroupeEnAttente { get; set; }
    public int AvisNonLus { get; set; }
    public int TotalCompetences { get; set; }
    public int TotalProjetsAGR { get; set; }
    public int TotalPartenaires { get; set; }
    public decimal SoldeFinancier { get; set; }
    public decimal TotalRecettes { get; set; }
    public decimal TotalDepenses { get; set; }
    public int MesActivites { get; set; }
    public int MesDemandes { get; set; }
    public int MesFormations { get; set; }
    public int MesCompetences { get; set; }
    public int MesCertificats { get; set; }
    public int SessionsLmsAVenir { get; set; }
    public int ParcoursCertifiants { get; set; }
    public int DiscussionsLmsActives { get; set; }
    public int AnnoncesLmsActives { get; set; }
    public double ProgressionLmsMoyenne { get; set; }
    public int MesEnfants { get; set; }
    public int ActivitesFamille { get; set; }
    public int FormationsFamille { get; set; }
    public decimal CotisationsFamille { get; set; }
    public List<GroupeDto> DerniersGroupes { get; set; } = [];
    public List<ActiviteDto> DernieresActivites { get; set; } = [];
    public List<DashboardFormationItemDto> DernieresFormations { get; set; } = [];
    public List<LmsParcoursItemDto> ParcoursLms { get; set; } = [];
    public List<AnnonceFormationDto> DernieresAnnoncesFormation { get; set; } = [];
}

public class DashboardFormationItemDto
{
    public Guid FormationId { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? SessionTitre { get; set; }
    public string? SessionStatut { get; set; }
    public bool EstSessionSelfPaced { get; set; }
    public int ProgressionPourcent { get; set; }
    public StatutInscription Statut { get; set; }
    public string? NomScout { get; set; }
}
