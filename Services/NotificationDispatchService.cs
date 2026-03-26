using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MangoTaika.Services;

public class NotificationDispatchService(AppDbContext db, IHubContext<NotificationHub> hubContext) : INotificationDispatchService
{
    public async Task SendAsync(IEnumerable<Guid> userIds, string title, string message, string category, string? link = null)
    {
        var recipients = userIds
            .Distinct()
            .ToList();

        if (recipients.Count == 0)
            return;

        var now = DateTime.UtcNow;
        db.NotificationsUtilisateur.AddRange(recipients.Select(userId => new NotificationUtilisateur
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Titre = title,
            Message = message,
            Categorie = category,
            Lien = link,
            EstLue = false,
            DateCreation = now
        }));

        await db.SaveChangesAsync();

        foreach (var userId in recipients)
        {
            await hubContext.Clients.User(userId.ToString()).SendAsync("RecevoirNotification", message);
        }
    }
}
