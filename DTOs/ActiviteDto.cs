using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class ActiviteDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeActivite Type { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Lieu { get; set; }
    public decimal? BudgetPrevisionnel { get; set; }
    public string? NomResponsable { get; set; }
    public StatutActivite Statut { get; set; }
    public string? MotifRejet { get; set; }
    public DateTime? DateCloturePointage { get; set; }
    public bool PointageCloture => DateCloturePointage.HasValue;
    public string? NomGroupe { get; set; }
    public Guid? GroupeId { get; set; }
    public string? NomCreateur { get; set; }
    public DateTime DateCreation { get; set; }
    public int NbParticipants { get; set; }
    public int NbDocuments { get; set; }
    public List<DocumentActiviteDto> Documents { get; set; } = [];
    public List<ParticipantActiviteDto> Participants { get; set; } = [];
    public List<CommentaireActiviteDto> Commentaires { get; set; } = [];
}

public class ActiviteCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeActivite Type { get; set; } = TypeActivite.Autre;
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Lieu { get; set; }
    public decimal? BudgetPrevisionnel { get; set; }
    public string? NomResponsable { get; set; }
    public Guid? GroupeId { get; set; }
}

public class DocumentActiviteDto
{
    public Guid Id { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public string CheminFichier { get; set; } = string.Empty;
    public string? TypeDocument { get; set; }
    public DateTime DateUpload { get; set; }
}

public class ParticipantActiviteDto
{
    public Guid Id { get; set; }
    public Guid ScoutId { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string Matricule { get; set; } = string.Empty;
    public string? NomBranche { get; set; }
    public StatutPresence Presence { get; set; }
}

public class CommentaireActiviteDto
{
    public Guid Id { get; set; }
    public string NomAuteur { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public string? TypeAction { get; set; }
    public DateTime DateCreation { get; set; }
}

public class PresenceScoutScanRequest
{
    public string ScannedCode { get; set; } = string.Empty;
}

public class PresenceScoutAddRequest
{
    public Guid ScoutId { get; set; }
}

public class PresenceScoutScanResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ParticipantId { get; set; }
    public Guid? ScoutId { get; set; }
    public bool CanAddParticipant { get; set; }
    public string? ScoutName { get; set; }
    public string? Matricule { get; set; }
    public string? PreviousPresence { get; set; }
    public string? CurrentPresence { get; set; }
    public int Presents { get; set; }
    public int Absents { get; set; }
    public int Excuses { get; set; }
    public int Pending { get; set; }
}
