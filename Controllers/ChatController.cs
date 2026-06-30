using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Hubs;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Route("Chat")]
public class ChatController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IChatBotService chatBotService,
    IChatBotIntentService chatBotIntentService,
    ITicketService ticketService,
    INotificationDispatchService notificationDispatchService,
    IHubContext<ChatHub> hubContext) : Controller
{
    private Guid CurrentUserId => Guid.Parse(userManager.GetUserId(User)!);

    private async Task<(ChatConversation Conversation, bool EstNouvelle)> DemarrerOuReprendreConversationAsync()
    {
        var existante = await db.ChatConversations
            .Where(c => c.VisiteurId == CurrentUserId && c.Statut != StatutChatConversation.Fermee)
            .OrderByDescending(c => c.DateCreation)
            .FirstOrDefaultAsync();

        if (existante != null) return (existante, false);

        var conversation = new ChatConversation { VisiteurId = CurrentUserId };
        db.ChatConversations.Add(conversation);
        await db.SaveChangesAsync();
        return (conversation, true);
    }

    private async Task NotifierAgentsAsync(string titre, string message, Guid conversationId)
    {
        var agents = await userManager.GetUsersInRoleAsync("AgentSupport");
        var admins = await userManager.GetUsersInRoleAsync("Administrateur");
        var destinataires = agents.Concat(admins).Select(u => u.Id).Distinct();

        await notificationDispatchService.SendAsync(
            destinataires,
            titre,
            message,
            "Support",
            $"/Chat/AdminInbox?conversation={conversationId}");
    }

    [HttpPost("Ask")]
    public async Task<IActionResult> Ask([FromForm] string message)
    {
        Guid? userId = User.Identity?.IsAuthenticated == true ? CurrentUserId : null;
        var estAgent = User.IsInRole("AgentSupport") || User.IsInRole("Administrateur");
        var reponse = await chatBotIntentService.InterpreterAsync(message ?? string.Empty, userId, estAgent);
        return Json(reponse);
    }

    [Authorize]
    [HttpGet("Bot/Tickets")]
    public async Task<IActionResult> BotTickets()
        => Json(await chatBotService.GetMesTicketsAsync(CurrentUserId));

    [Authorize]
    [HttpGet("Bot/Demandes")]
    public async Task<IActionResult> BotDemandes()
        => Json(await chatBotService.GetMesDemandesAsync(CurrentUserId));

    [Authorize]
    [HttpGet("Bot/Activites")]
    public async Task<IActionResult> BotActivites()
        => Json(await chatBotService.GetMesActivitesAsync(CurrentUserId));

    [HttpGet("Faq")]
    public async Task<IActionResult> Faq(string? q)
    {
        var resultats = await chatBotService.RechercherFaqAsync(q ?? string.Empty);
        return Json(resultats.Select(r => new { id = r.Id, question = r.Question, reponse = r.Reponse }));
    }

    [HttpGet("Bot/Boutique")]
    public async Task<IActionResult> BotBoutique(string? q)
        => Json(await chatBotService.GetArticlesBoutiqueAsync(q));

    [HttpGet("Bot/ActivitesPubliques")]
    public async Task<IActionResult> BotActivitesPubliques()
        => Json(await chatBotService.GetActivitesPubliquesAsync());

    [HttpGet("Bot/Actualites")]
    public async Task<IActionResult> BotActualites()
        => Json(await chatBotService.GetActualitesRecentesAsync());

    [Authorize]
    [HttpPost("Start")]
    public async Task<IActionResult> Start()
    {
        var (conversation, estNouvelle) = await DemarrerOuReprendreConversationAsync();
        var nomVisiteur = User.Identity?.Name ?? "Un visiteur";

        await hubContext.Clients.Group("ChatAgents").SendAsync("NouvelleConversation", new
        {
            conversationId = conversation.Id,
            visiteur = nomVisiteur,
            date = conversation.DateCreation
        });

        if (estNouvelle)
        {
            await NotifierAgentsAsync(
                "Nouvelle discussion en attente",
                $"{nomVisiteur} souhaite parler à un agent.",
                conversation.Id);
        }

        return Json(new { conversationId = conversation.Id });
    }

    [Authorize]
    [HttpPost("CreerTicketDepuisChat")]
    public async Task<IActionResult> CreerTicketDepuisChat([FromForm] string probleme)
    {
        if (string.IsNullOrWhiteSpace(probleme))
            return BadRequest();

        var sujet = probleme.Length > 80 ? probleme[..80] + "..." : probleme;
        var ticket = await ticketService.CreateAsync(new TicketCreateDto
        {
            Sujet = sujet,
            Description = probleme + "\n\n(Signalé via l'assistant de chat — l'aide rapide n'a pas résolu le problème.)",
            Type = TypeTicket.Incident,
            Categorie = CategorieTicket.Technique
        }, CurrentUserId);

        var (conversation, _) = await DemarrerOuReprendreConversationAsync();
        var nomVisiteur = User.Identity?.Name ?? "Un visiteur";

        var messageSysteme = new ChatMessage
        {
            ConversationId = conversation.Id,
            ExpediteurId = null,
            EstBot = true,
            Contenu = $"Ticket {ticket.NumeroTicket} créé automatiquement : « {sujet} ». Un agent va prendre le relais."
        };
        db.ChatMessages.Add(messageSysteme);
        await db.SaveChangesAsync();

        await hubContext.Clients.Group("ChatAgents").SendAsync("NouvelleConversation", new
        {
            conversationId = conversation.Id,
            visiteur = nomVisiteur,
            date = conversation.DateCreation,
            numeroTicket = ticket.NumeroTicket
        });

        await hubContext.Clients.Group($"chat-conv-{conversation.Id}").SendAsync("NouveauMessage", new
        {
            conversationId = conversation.Id,
            contenu = messageSysteme.Contenu,
            expediteurId = (Guid?)null,
            expediteurNom = "Assistant",
            date = messageSysteme.DateEnvoi
        });

        await NotifierAgentsAsync(
            $"Nouveau ticket {ticket.NumeroTicket} — assistance requise",
            $"{nomVisiteur} a un problème non résolu par l'assistant : « {sujet} ». Ticket {ticket.NumeroTicket} créé automatiquement.",
            conversation.Id);

        return Json(new { conversationId = conversation.Id, numeroTicket = ticket.NumeroTicket });
    }

    [Authorize]
    [HttpGet("History/{conversationId:guid}")]
    public async Task<IActionResult> History(Guid conversationId)
    {
        var conversation = await db.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation is null)
            return NotFound();

        var estAgent = User.IsInRole("AgentSupport") || User.IsInRole("Administrateur");
        if (!estAgent && conversation.VisiteurId != CurrentUserId)
            return Forbid();

        var messages = conversation.Messages
            .OrderBy(m => m.DateEnvoi)
            .Select(m => new
            {
                id = m.Id,
                contenu = m.Contenu,
                estBot = m.EstBot,
                estMoi = m.ExpediteurId == CurrentUserId,
                date = m.DateEnvoi
            });

        return Json(new { conversation.Statut, messages });
    }

    [Authorize]
    [HttpPost("Send/{conversationId:guid}")]
    public async Task<IActionResult> Send(Guid conversationId, [FromForm] string contenu)
    {
        if (string.IsNullOrWhiteSpace(contenu))
            return BadRequest();

        var conversation = await db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conversation is null)
            return NotFound();

        var estAgent = User.IsInRole("AgentSupport") || User.IsInRole("Administrateur");
        if (!estAgent && conversation.VisiteurId != CurrentUserId)
            return Forbid();

        var message = new ChatMessage
        {
            ConversationId = conversationId,
            ExpediteurId = CurrentUserId,
            Contenu = contenu.Trim()
        };
        db.ChatMessages.Add(message);

        if (estAgent && conversation.AgentId is null)
        {
            conversation.AgentId = CurrentUserId;
            conversation.Statut = StatutChatConversation.EnCours;
        }

        await db.SaveChangesAsync();

        await hubContext.Clients.Group($"chat-conv-{conversationId}").SendAsync("NouveauMessage", new
        {
            conversationId,
            contenu = message.Contenu,
            expediteurId = message.ExpediteurId,
            expediteurNom = User.Identity?.Name,
            date = message.DateEnvoi
        });

        return Json(new { ok = true });
    }

    [HttpPost("Close/{conversationId:guid}")]
    [Authorize(Roles = "AgentSupport,Administrateur")]
    public async Task<IActionResult> Close(Guid conversationId)
    {
        var conversation = await db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conversation is null)
            return NotFound();

        conversation.Statut = StatutChatConversation.Fermee;
        conversation.DateFermeture = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await hubContext.Clients.Group($"chat-conv-{conversationId}").SendAsync("ConversationFermee", new { conversationId });

        return Json(new { ok = true });
    }

    [HttpGet("Inbox")]
    [Authorize(Roles = "AgentSupport,Administrateur")]
    public async Task<IActionResult> Inbox()
    {
        var conversations = await db.ChatConversations
            .Include(c => c.Visiteur)
            .Where(c => c.Statut != StatutChatConversation.Fermee)
            .OrderByDescending(c => c.DateCreation)
            .Select(c => new
            {
                id = c.Id,
                visiteur = c.Visiteur.Prenom + " " + c.Visiteur.Nom,
                statut = c.Statut.ToString(),
                date = c.DateCreation,
                dernierMessage = c.Messages.OrderByDescending(m => m.DateEnvoi).Select(m => m.Contenu).FirstOrDefault()
            })
            .ToListAsync();

        return Json(conversations);
    }

    [HttpGet("AdminInbox")]
    [Authorize(Roles = "AgentSupport,Administrateur")]
    public IActionResult AdminInbox() => View();
}
