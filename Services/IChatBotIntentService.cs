namespace MangoTaika.Services;

public record ChatBotCard(string Titre, string? SousTitre, string? Badge);

public record ChatBotReplyDto(string Texte, string Intention, List<ChatBotCard>? Items = null, string? ProblemeOriginal = null);

public interface IChatBotIntentService
{
    Task<ChatBotReplyDto> InterpreterAsync(string message, Guid? userId, bool estAgent);
}
