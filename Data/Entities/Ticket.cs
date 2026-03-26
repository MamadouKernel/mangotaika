namespace MangoTaika.Data.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string NumeroTicket { get; set; } = string.Empty;
    public string Sujet { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TypeTicket Type { get; set; } = TypeTicket.Requete;
    public CategorieTicket Categorie { get; set; } = CategorieTicket.Autre;
    public ImpactTicket Impact { get; set; } = ImpactTicket.Moyen;
    public UrgenceTicket Urgence { get; set; } = UrgenceTicket.Moyenne;
    public PrioriteTicket Priorite { get; set; } = PrioriteTicket.Normale;
    public StatutTicket Statut { get; set; } = StatutTicket.Ouvert;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateLimiteSla { get; set; } = DateTime.UtcNow.AddHours(24);
    public DateTime? DatePremiereReponse { get; set; }
    public DateTime? DateAffectation { get; set; }
    public DateTime? DateResolution { get; set; }
    public bool EstEscalade { get; set; }
    public int NiveauEscalade { get; set; }
    public DateTime? DateDerniereEscalade { get; set; }
    public string? ResumeResolution { get; set; }
    public int? NoteSatisfaction { get; set; }
    public string? CommentaireSatisfaction { get; set; }
    public bool EstSupprime { get; set; } = false;
    public Guid? ServiceCatalogueId { get; set; }

    // Navigation
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public Guid? AssigneAId { get; set; }
    public ApplicationUser? AssigneA { get; set; }
    public Guid? GroupeAssigneId { get; set; }
    public Groupe? GroupeAssigne { get; set; }
    public SupportServiceCatalogueItem? ServiceCatalogue { get; set; }
    public ICollection<MessageTicket> Messages { get; set; } = [];
    public ICollection<HistoriqueTicket> Historiques { get; set; } = [];
    public ICollection<TicketPieceJointe> PiecesJointes { get; set; } = [];
}

public enum TypeTicket { Incident, Requete }
public enum CategorieTicket { Technique, Administrative, Activites, Adhesion, Autre }
public enum ImpactTicket { Faible, Moyen, Eleve }
public enum UrgenceTicket { Faible, Moyenne, Haute, Critique }
public enum PrioriteTicket { Basse, Normale, Haute, Urgente }
public enum StatutTicket
{
    Nouveau = 0,
    Ouvert = 0,
    Affecte = 1,
    EnCours = 1,
    EnAttente = 2,
    Resolu = 3,
    Ferme = 4,
    EnAttenteDemandeur = 5,
    EnAttenteTiers = 6,
    Annule = 7
}

public class TicketPieceJointe
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string NomOriginal { get; set; } = string.Empty;
    public string UrlFichier { get; set; } = string.Empty;
    public string? TypeMime { get; set; }
    public long TailleOctets { get; set; }
    public DateTime DateAjout { get; set; } = DateTime.UtcNow;
    public Guid AjouteParId { get; set; }
    public ApplicationUser AjoutePar { get; set; } = null!;
}

public class SupportServiceCatalogueItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TypeTicket TypeParDefaut { get; set; } = TypeTicket.Requete;
    public CategorieTicket CategorieParDefaut { get; set; } = CategorieTicket.Autre;
    public ImpactTicket ImpactParDefaut { get; set; } = ImpactTicket.Moyen;
    public UrgenceTicket UrgenceParDefaut { get; set; } = UrgenceTicket.Moyenne;
    public int DelaiSlaHeures { get; set; } = 24;
    public Guid? AssigneParDefautId { get; set; }
    public ApplicationUser? AssigneParDefaut { get; set; }
    public Guid? GroupeParDefautId { get; set; }
    public Groupe? GroupeParDefaut { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public Guid? AuteurId { get; set; }
    public ApplicationUser? Auteur { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = [];
}

public class SupportKnowledgeArticle
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Resume { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public string? MotsCles { get; set; }
    public bool EstPublie { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateMiseAJour { get; set; }
    public Guid? AuteurId { get; set; }
    public ApplicationUser? Auteur { get; set; }
}
