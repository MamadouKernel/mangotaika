namespace MangoTaika.Services;

public interface IEmailNotificationService
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        string? recipientName = null,
        string? category = null,
        string? link = null);
}
