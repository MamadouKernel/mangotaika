namespace MangoTaika.Services;

public record ChatBotItemDto(Guid Id, string Titre, string Statut, DateTime Date);
public record ChatBotArticleDto(Guid Id, string Nom, string? Categorie, decimal Prix, string Devise, bool EnStock);
public record ChatBotActualiteDto(Guid Id, string Titre, string? Resume, DateTime DatePublication);

public interface IChatBotService
{
    Task<List<ChatBotItemDto>> GetMesTicketsAsync(Guid userId);
    Task<List<ChatBotItemDto>> GetMesDemandesAsync(Guid userId);
    Task<List<ChatBotItemDto>> GetMesActivitesAsync(Guid userId);
    Task<List<(Guid Id, string Question, string Reponse)>> RechercherFaqAsync(string motsCles);

    Task<List<ChatBotArticleDto>> GetArticlesBoutiqueAsync(string? recherche = null);
    Task<List<ChatBotItemDto>> GetActivitesPubliquesAsync();
    Task<List<ChatBotActualiteDto>> GetActualitesRecentesAsync();
}
