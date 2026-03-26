using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class FormationDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public NiveauFormation Niveau { get; set; }
    public StatutFormation Statut { get; set; }
    public int DureeEstimeeHeures { get; set; }
    public DateTime DateCreation { get; set; }
    public string? NomBrancheCible { get; set; }
    public Guid? BrancheCibleId { get; set; }
    public Guid? CompetenceLieeId { get; set; }
    public string NomAuteur { get; set; } = string.Empty;
    public int NombreModules { get; set; }
    public int NombreInscrits { get; set; }
    public bool DelivreBadge { get; set; }
    public bool DelivreAttestation { get; set; }
    public bool DelivreCertificat { get; set; }
    public Guid? SessionId { get; set; }
    public string? SessionTitre { get; set; }
    public string? SessionStatut { get; set; }
    public bool EstSessionSelfPaced { get; set; }
    public DateTime? DateOuvertureSession { get; set; }
    public DateTime? DateFermetureSession { get; set; }
    public int NombreAnnoncesPubliees { get; set; }
    public int NombreDiscussions { get; set; }
    public DateTime? DateDerniereActiviteForum { get; set; }
    public List<PrerequisFormationDto> Prerequis { get; set; } = [];
    public bool PeutSInscrire { get; set; } = true;
    public int NombrePrerequisRestants { get; set; }
    public string? MessageInscription { get; set; }
    public DateTime? ProchainJalonDate { get; set; }
    public string? ProchainJalonTitre { get; set; }
}

public class FormationCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public NiveauFormation Niveau { get; set; } = NiveauFormation.Debutant;
    public int DureeEstimeeHeures { get; set; }
    public Guid? BrancheCibleId { get; set; }
    public Guid? CompetenceLieeId { get; set; }
    public bool DelivreBadge { get; set; } = true;
    public bool DelivreAttestation { get; set; } = true;
    public bool DelivreCertificat { get; set; }
    public List<Guid> PrerequisFormationIds { get; set; } = [];
}

public class FormationDetailDto : FormationDto
{
    public List<ModuleDto> Modules { get; set; } = [];
    public List<SessionFormationDto> Sessions { get; set; } = [];
    public List<AnnonceFormationDto> Annonces { get; set; } = [];
    public List<JalonFormationDto> Jalons { get; set; } = [];
    public List<DiscussionFormationDto> DiscussionsRecentes { get; set; } = [];
}

public class ModuleDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Ordre { get; set; }
    public int NombreLecons { get; set; }
    public bool AQuiz { get; set; }
    public List<LeconDto> Lecons { get; set; } = [];
    public QuizDto? Quiz { get; set; }
}

public class LeconDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public TypeLecon Type { get; set; }
    public string? ContenuTexte { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public int Ordre { get; set; }
    public int DureeMinutes { get; set; }
}

public class QuizDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int NoteMinimale { get; set; }
    public int? NombreTentativesMax { get; set; }
    public DateTime? DateOuvertureDisponibilite { get; set; }
    public DateTime? DateFermetureDisponibilite { get; set; }
    public List<QuestionDto> Questions { get; set; } = [];
}

public class QuizTentativeDto
{
    public Guid Id { get; set; }
    public int Score { get; set; }
    public bool Reussi { get; set; }
    public DateTime DateTentative { get; set; }
}

public class QuizPassagePageDto
{
    public Guid FormationId { get; set; }
    public QuizDto Quiz { get; set; } = null!;
    public List<QuizTentativeDto> Tentatives { get; set; } = [];
    public int? MeilleurScore { get; set; }
    public int NombreTentatives { get; set; }
    public int? NombreTentativesRestantes { get; set; }
    public string EtatEvaluation { get; set; } = string.Empty;
    public bool PeutSoumettre { get; set; }
    public bool EstLectureSeule { get; set; }
    public string? MessageAcces { get; set; }
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Enonce { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public List<ReponseDto> Reponses { get; set; } = [];
}

public class ReponseDto
{
    public Guid Id { get; set; }
    public string Texte { get; set; } = string.Empty;
    public bool EstCorrecte { get; set; }
    public int Ordre { get; set; }
}

public class ModuleCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class LeconCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public TypeLecon Type { get; set; } = TypeLecon.Texte;
    public string? ContenuTexte { get; set; }
    public string? VideoUrl { get; set; }
    public int DureeMinutes { get; set; }
}

public class QuestionCreateDto
{
    public string Enonce { get; set; } = string.Empty;
    public List<ReponseCreateDto> Reponses { get; set; } = [];
}

public class ReponseCreateDto
{
    public string Texte { get; set; } = string.Empty;
    public bool EstCorrecte { get; set; }
}

// Vue scout : progression dans une formation
public class FormationProgressionDto
{
    public FormationDetailDto Formation { get; set; } = null!;
    public int PourcentageGlobal { get; set; }
    public List<ModuleProgressionDto> Modules { get; set; } = [];
    public HashSet<Guid> LeconsTermineesIds { get; set; } = [];
    public int NombreModulesTermines { get; set; }
    public int NombreModulesTotal { get; set; }
    public int NombreQuizReussis { get; set; }
    public int NombreQuizTotal { get; set; }
    public int? MeilleurScoreGlobal { get; set; }
    public bool BadgeObtenu { get; set; }
    public bool AttestationObtenue { get; set; }
    public bool CertificatObtenu { get; set; }
    public string EtatPedagogique { get; set; } = string.Empty;
    public string EtatEvaluation { get; set; } = string.Empty;
    public string EtatCertifiant { get; set; } = string.Empty;
    public string ProchaineEtape { get; set; } = string.Empty;
    public bool PeutInteragir { get; set; }
    public bool EstLectureSeule { get; set; }
    public string? MessageAcces { get; set; }
    public List<NotificationLmsDto> NotificationsLms { get; set; } = [];
}

public class ModuleProgressionDto
{
    public ModuleDto Module { get; set; } = null!;
    public int LeconsTerminees { get; set; }
    public int TotalLecons { get; set; }
    public bool QuizReussi { get; set; }
    public int? MeilleurScore { get; set; }
    public int NombreTentativesQuiz { get; set; }
    public int? TentativesRestantesQuiz { get; set; }
    public DateTime? DateDerniereTentativeQuiz { get; set; }
    public List<QuizTentativeDto> TentativesQuiz { get; set; } = [];
    public bool EstDisponible { get; set; }
    public string? MessageBlocage { get; set; }
    public HashSet<Guid> LeconsDisponiblesIds { get; set; } = [];
    public bool QuizDisponible { get; set; }
    public string? MessageQuiz { get; set; }
}

public class SessionFormationDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool EstSelfPaced { get; set; }
    public bool EstPubliee { get; set; }
    public string StatutAffichage { get; set; } = string.Empty;
    public DateTime? DateOuverture { get; set; }
    public DateTime? DateFermeture { get; set; }
}

public class SessionFormationCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool EstSelfPaced { get; set; }
    public bool EstPubliee { get; set; } = true;
    public DateTime? DateOuverture { get; set; }
    public DateTime? DateFermeture { get; set; }
}

public class AnnonceFormationDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public bool EstPubliee { get; set; }
    public DateTime DatePublication { get; set; }
    public string? NomAuteur { get; set; }
}

public class AnnonceFormationCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public bool EstPubliee { get; set; } = true;
}

public class PrerequisFormationDto
{
    public Guid FormationId { get; set; }
    public string Titre { get; set; } = string.Empty;
    public bool EstValide { get; set; }
}

public class JalonFormationDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateJalon { get; set; }
    public TypeJalonFormation Type { get; set; }
    public bool EstPublie { get; set; }
}

public class JalonFormationCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateJalon { get; set; }
    public TypeJalonFormation Type { get; set; } = TypeJalonFormation.Rappel;
    public bool EstPublie { get; set; } = true;
}

public class NotificationLmsDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public string? Lien { get; set; }
    public bool EstLue { get; set; }
    public DateTime DateCreation { get; set; }
}

public class CertificationFormationDto
{
    public Guid Id { get; set; }
    public TypeCertificationFormation Type { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime DateEmission { get; set; }
    public int ScoreFinal { get; set; }
    public string Mention { get; set; } = string.Empty;
    public Guid FormationId { get; set; }
    public string FormationTitre { get; set; } = string.Empty;
    public string NomScout { get; set; } = string.Empty;
}

public class DiscussionFormationDto
{
    public Guid Id { get; set; }
    public Guid FormationId { get; set; }
    public string FormationTitre { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string ContenuInitial { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime DateDerniereActivite { get; set; }
    public bool EstVerrouillee { get; set; }
    public string NomAuteur { get; set; } = string.Empty;
    public int NombreMessages { get; set; }
}

public class MessageDiscussionFormationDto
{
    public Guid Id { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public string NomAuteur { get; set; } = string.Empty;
}

public class DiscussionFormationDetailDto : DiscussionFormationDto
{
    public List<MessageDiscussionFormationDto> Messages { get; set; } = [];
}

public class DiscussionFormationCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
}

public class MessageDiscussionFormationCreateDto
{
    public string Contenu { get; set; } = string.Empty;
}

public class ForumFormationPageDto
{
    public Guid FormationId { get; set; }
    public string FormationTitre { get; set; } = string.Empty;
    public bool LectureSeule { get; set; }
    public bool PeutParticiper { get; set; }
    public bool PeutModerer { get; set; }
    public List<DiscussionFormationDto> Discussions { get; set; } = [];
}

public class DiscussionFormationPageDto
{
    public Guid FormationId { get; set; }
    public string FormationTitre { get; set; } = string.Empty;
    public bool LectureSeule { get; set; }
    public bool PeutParticiper { get; set; }
    public bool PeutModerer { get; set; }
    public DiscussionFormationDetailDto Discussion { get; set; } = null!;
}

public class LmsParcoursItemDto
{
    public Guid FormationId { get; set; }
    public Guid ScoutId { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? NomScout { get; set; }
    public string? SessionTitre { get; set; }
    public string? SessionStatut { get; set; }
    public bool EstSessionSelfPaced { get; set; }
    public DateTime? DateOuvertureSession { get; set; }
    public DateTime? DateFermetureSession { get; set; }
    public int ProgressionPourcent { get; set; }
    public StatutInscription Statut { get; set; }
    public int NombreModules { get; set; }
    public int NombreModulesTermines { get; set; }
    public int NombreQuiz { get; set; }
    public int NombreQuizReussis { get; set; }
    public int? MeilleurScoreQuiz { get; set; }
    public int NombreAnnonces { get; set; }
    public int NombreDiscussions { get; set; }
    public DateTime? DerniereActiviteCours { get; set; }
    public DateTime? ProchainJalonDate { get; set; }
    public string? ProchainJalonTitre { get; set; }
    public bool DelivreBadge { get; set; }
    public bool DelivreAttestation { get; set; }
    public bool DelivreCertificat { get; set; }
    public bool BadgeObtenu { get; set; }
    public bool AttestationObtenue { get; set; }
    public bool CertificatObtenu { get; set; }
    public int NombreCertificationsObtenues { get; set; }
    public string EtatPedagogique { get; set; } = string.Empty;
    public string EtatEvaluation { get; set; } = string.Empty;
    public string EtatCertifiant { get; set; } = string.Empty;
    public string ProchaineEtape { get; set; } = string.Empty;
}
