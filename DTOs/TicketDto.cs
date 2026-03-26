using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class TicketDto
{
    public Guid Id { get; set; }
    public string NumeroTicket { get; set; } = string.Empty;
    public Guid? ServiceCatalogueId { get; set; }
    public string? NomServiceCatalogue { get; set; }
    public string Sujet { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TypeTicket Type { get; set; }
    public CategorieTicket Categorie { get; set; }
    public ImpactTicket Impact { get; set; }
    public UrgenceTicket Urgence { get; set; }
    public PrioriteTicket Priorite { get; set; }
    public StatutTicket Statut { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateResolution { get; set; }
    public DateTime? DatePremiereReponse { get; set; }
    public DateTime DateLimiteSla { get; set; }
    public DateTime? DateAffectation { get; set; }
    public bool EstEnRetard { get; set; }
    public double HeuresAvantSla { get; set; }
    public bool EstEscalade { get; set; }
    public int NiveauEscalade { get; set; }
    public DateTime? DateDerniereEscalade { get; set; }
    public string? ResumeResolution { get; set; }
    public int? NoteSatisfaction { get; set; }
    public string? CommentaireSatisfaction { get; set; }
    public string? NomCreateur { get; set; }
    public Guid CreateurId { get; set; }
    public string? NomAssigne { get; set; }
    public Guid? AssigneAId { get; set; }
    public Guid? GroupeAssigneId { get; set; }
    public string? NomGroupeAssigne { get; set; }
    public List<MessageTicketDto> Messages { get; set; } = [];
    public List<HistoriqueTicketDto> Historiques { get; set; } = [];
    public List<TicketAttachmentDto> PiecesJointes { get; set; } = [];
}

public class MessageTicketDto
{
    public Guid Id { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public DateTime DateEnvoi { get; set; }
    public string NomAuteur { get; set; } = string.Empty;
    public Guid AuteurId { get; set; }
    public bool EstSupport { get; set; }
    public bool EstNoteInterne { get; set; }
}

public class TicketCreateDto
{
    public Guid? ServiceCatalogueId { get; set; }
    public string Sujet { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TypeTicket Type { get; set; } = TypeTicket.Requete;
    public CategorieTicket Categorie { get; set; } = CategorieTicket.Autre;
    public ImpactTicket Impact { get; set; } = ImpactTicket.Moyen;
    public UrgenceTicket Urgence { get; set; } = UrgenceTicket.Moyenne;
    public PrioriteTicket Priorite { get; set; } = PrioriteTicket.Normale;
}

public class TicketAttachmentDto
{
    public Guid Id { get; set; }
    public string NomOriginal { get; set; } = string.Empty;
    public string? TypeMime { get; set; }
    public long TailleOctets { get; set; }
    public DateTime DateAjout { get; set; }
    public string NomAjoutePar { get; set; } = string.Empty;
}

public class TicketResolutionDto
{
    public Guid TicketId { get; set; }
    public string ResumeResolution { get; set; } = string.Empty;
    public bool FermerApresResolution { get; set; }
}

public class HistoriqueTicketDto
{
    public StatutTicket AncienStatut { get; set; }
    public StatutTicket NouveauStatut { get; set; }
    public string? NomAuteur { get; set; }
    public string? Commentaire { get; set; }
    public DateTime DateChangement { get; set; }
}

public class SupportDashboardDto
{
    public int TicketsOuverts { get; set; }
    public int TicketsEnCours { get; set; }
    public int TicketsEnAttente { get; set; }
    public int TicketsNonAssignes { get; set; }
    public int TicketsEnRetard { get; set; }
    public int MesTicketsAssignes { get; set; }
    public int TicketsResolusAujourdHui { get; set; }
    public double TempsMoyenResolutionHeures { get; set; }
    public double TempsMoyenPremiereReponseHeures { get; set; }
    public double NoteMoyenneSatisfaction { get; set; }
    public int TotalTickets { get; set; }
    public List<SupportAgentStatDto> StatistiquesAgents { get; set; } = [];
}

public class SupportAgentStatDto
{
    public Guid AgentId { get; set; }
    public string NomAgent { get; set; } = string.Empty;
    public int TicketsActifs { get; set; }
    public int TicketsResolus { get; set; }
    public int TicketsEnRetard { get; set; }
    public double TempsMoyenPremiereReponseHeures { get; set; }
    public double TempsMoyenResolutionHeures { get; set; }
    public double NoteMoyenneSatisfaction { get; set; }
}
