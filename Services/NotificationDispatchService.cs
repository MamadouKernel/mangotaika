using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace MangoTaika.Services;

public class NotificationDispatchService(
    AppDbContext db,
    IHubContext<NotificationHub> hubContext,
    IEmailNotificationService emailService,
    ISmsService smsService,
    IConfiguration configuration,
    ILogger<NotificationDispatchService> logger) : INotificationDispatchService
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

        var contacts = await db.Users
            .Where(u => recipients.Contains(u.Id))
            .Select(u => new { u.Email, u.PhoneNumber })
            .ToListAsync();

        foreach (var contact in contacts)
        {
            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                try
                {
                    await emailService.SendAsync(contact.Email, title, $"{message}\n\n{link ?? string.Empty}".Trim());
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Email notification non envoyee vers {Email}", contact.Email);
                }
            }

            if (ShouldSendSms(category) && !string.IsNullOrWhiteSpace(contact.PhoneNumber))
            {
                try
                {
                    await smsService.SendSmsAsync(contact.PhoneNumber, $"{title}: {message}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "SMS notification non envoyee vers {Phone}", contact.PhoneNumber);
                }
            }
        }

        foreach (var userId in recipients)
        {
            await hubContext.Clients.User(userId.ToString()).SendAsync("RecevoirNotification", message);
        }
    }

    private bool ShouldSendSms(string category)
    {
        var configured = configuration.GetSection("Notifications:SmsCategories").Get<string[]>() ?? ["Security", "Activites", "LMS"];
        return configured.Contains(category, StringComparer.OrdinalIgnoreCase);
    }
}
