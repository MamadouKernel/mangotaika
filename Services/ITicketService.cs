using MangoTaika.Data.Entities;
using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface ITicketService
{
    Task<List<TicketDto>> GetAllAsync(
        StatutTicket? statut = null,
        TypeTicket? type = null,
        CategorieTicket? categorie = null,
        PrioriteTicket? priorite = null,
        string? vue = null,
        string? recherche = null,
        Guid? agentId = null);
    Task<TicketDto?> GetByIdAsync(Guid id);
    Task<List<TicketDto>> GetByUserAsync(Guid userId);
    Task<TicketDto> CreateAsync(TicketCreateDto dto, Guid createurId);
    Task<bool> UpdateStatutAsync(Guid id, StatutTicket statut, Guid? auteurId = null);
    Task<bool> AssignerAsync(Guid ticketId, Guid userId);
    Task<MessageTicketDto> AjouterMessageAsync(Guid ticketId, string contenu, Guid auteurId);
    Task<MessageTicketDto> AjouterNoteInterneAsync(Guid ticketId, string contenu, Guid auteurId);
    Task<bool> NoterAsync(Guid ticketId, int note, string? commentaire);
    Task<bool> RecategoriserAsync(Guid ticketId, TypeTicket type, CategorieTicket categorie, ImpactTicket impact, UrgenceTicket urgence);
    Task<bool> AssignerGroupeAsync(Guid ticketId, Guid groupeId);
    Task<bool> ResoudreAsync(Guid ticketId, string resumeResolution, bool fermerApresResolution, Guid auteurId);
    Task<SupportDashboardDto> GetSupportDashboardAsync(Guid? agentId = null);
}
