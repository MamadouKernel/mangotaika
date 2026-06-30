namespace MangoTaika.Data.Entities;

public class ChatConversation
{
    public Guid Id { get; set; }
    public Guid VisiteurId { get; set; }
    public ApplicationUser Visiteur { get; set; } = null!;
    public Guid? AgentId { get; set; }
    public ApplicationUser? Agent { get; set; }
    public StatutChatConversation Statut { get; set; } = StatutChatConversation.Ouverte;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateFermeture { get; set; }
    public bool EstSupprime { get; set; } = false;

    public ICollection<ChatMessage> Messages { get; set; } = [];
}

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public Guid? ExpediteurId { get; set; }
    public ApplicationUser? Expediteur { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public bool EstBot { get; set; } = false;
    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;
    public bool EstLu { get; set; } = false;
}

public enum StatutChatConversation
{
    Ouverte,
    EnCours,
    Fermee
}

public class FaqEntry
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Reponse { get; set; } = string.Empty;
    public string? MotsCles { get; set; }
    public string? Categorie { get; set; }
    public bool EstActif { get; set; } = true;
    public int OrdreAffichage { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
