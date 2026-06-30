using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MangoTaika.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const string AgentsGroup = "ChatAgents";

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task JoinAgentQueue()
    {
        if (Context.User?.IsInRole("AgentSupport") == true || Context.User?.IsInRole("Administrateur") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AgentsGroup);
        }
    }

    private static string ConversationGroup(string conversationId) => $"chat-conv-{conversationId}";
}
