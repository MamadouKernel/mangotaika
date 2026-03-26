using MangoTaika.Data.Entities;
using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface IFormationService
{
    // Admin CRUD
    Task<int> CountAsync();
    Task<List<FormationDto>> GetAllAsync();
    Task<List<FormationDto>> GetPageAsync(int skip, int take);
    Task<FormationDetailDto?> GetDetailAsync(Guid id);
    Task<Formation> CreateAsync(FormationCreateDto dto, Guid auteurId);
    Task<bool> UpdateAsync(Guid id, FormationCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> PublierAsync(Guid id);
    Task<bool> ArchiverAsync(Guid id);

    // Modules
    Task<ModuleFormation> AjouterModuleAsync(Guid formationId, ModuleCreateDto dto);
    Task<bool> UpdateModuleAsync(Guid moduleId, ModuleCreateDto dto);
    Task<bool> DeleteModuleAsync(Guid moduleId);

    // Leçons
    Task<Lecon> AjouterLeconAsync(Guid moduleId, LeconCreateDto dto);
    Task<bool> UpdateLeconAsync(Guid leconId, LeconCreateDto dto);
    Task<bool> DeleteLeconAsync(Guid leconId);

    // Quiz
    Task<Quiz> CreerQuizAsync(Guid moduleId, string titre, int noteMinimale, int? nombreTentativesMax = null, DateTime? dateOuvertureDisponibilite = null, DateTime? dateFermetureDisponibilite = null);
    Task<bool> UpdateQuizAsync(Guid quizId, string titre, int noteMinimale, int? nombreTentativesMax, DateTime? dateOuvertureDisponibilite, DateTime? dateFermetureDisponibilite);
    Task AjouterQuestionAsync(Guid quizId, QuestionCreateDto dto);
    Task<bool> DeleteQuestionAsync(Guid questionId);
    Task<bool> DeleteQuizAsync(Guid quizId);
    Task<SessionFormation> AjouterSessionAsync(Guid formationId, SessionFormationCreateDto dto);
    Task<bool> DeleteSessionAsync(Guid sessionId);
    Task<AnnonceFormation> AjouterAnnonceAsync(Guid formationId, AnnonceFormationCreateDto dto, Guid? auteurId);
    Task<bool> DeleteAnnonceAsync(Guid annonceId);
    Task<JalonFormation> AjouterJalonAsync(Guid formationId, JalonFormationCreateDto dto);
    Task<bool> DeleteJalonAsync(Guid jalonId);
    Task<List<DiscussionFormationDto>> GetDiscussionsAsync(Guid formationId);
    Task<DiscussionFormationDetailDto?> GetDiscussionAsync(Guid discussionId);
    Task<DiscussionFormation> AjouterDiscussionAsync(Guid formationId, Guid auteurId, DiscussionFormationCreateDto dto);
    Task<MessageDiscussionFormation> AjouterMessageDiscussionAsync(Guid discussionId, Guid auteurId, MessageDiscussionFormationCreateDto dto);
    Task<bool> BasculerVerrouDiscussionAsync(Guid discussionId);

    // Scout
    Task<List<FormationDto>> GetCatalogueAsync(Guid? brancheId, Guid? scoutId = null);
    Task<InscriptionFormation> InscrireScoutAsync(Guid formationId, Guid scoutId);
    Task<bool> EstInscritAsync(Guid formationId, Guid scoutId);
    Task<List<LmsParcoursItemDto>> GetParcoursScoutsAsync(IEnumerable<Guid> scoutIds);
    Task<bool> LeconAppartientFormationAsync(Guid leconId, Guid formationId);
    Task<bool> QuizAppartientFormationAsync(Guid quizId, Guid formationId);
    Task<QuizPassagePageDto?> GetQuizPassageAsync(Guid quizId, Guid formationId, Guid scoutId);
    Task<FormationProgressionDto?> GetProgressionAsync(Guid formationId, Guid scoutId);
    Task MarquerLeconTermineeAsync(Guid leconId, Guid scoutId);
    Task<TentativeQuiz> SoumettreQuizAsync(Guid quizId, Guid scoutId, Dictionary<Guid, Guid> reponses);
    Task<List<InscriptionFormation>> GetInscriptionsScoutAsync(Guid scoutId);

    // Stats
    Task<FormationStatsDto> GetStatsAsync(Guid formationId);
    Task<Dictionary<Guid, FormationStatsDto>> GetStatsByFormationAsync(IEnumerable<Guid> formationIds);
}
