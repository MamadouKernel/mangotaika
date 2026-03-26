using Microsoft.AspNetCore.SignalR;

namespace MangoTaika.Hubs;

public class NotificationHub : Hub
{
    public async Task EnvoyerNotification(string utilisateur, string message)
    {
        await Clients.User(utilisateur).SendAsync("RecevoirNotification", message);
    }

    public async Task EnvoyerATous(string message)
    {
        await Clients.All.SendAsync("RecevoirNotification", message);
    }
}
