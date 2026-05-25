using System.Net;
using System.Net.Mail;
using System.Text;

namespace MangoTaika.Services;

public class EmailNotificationService(ILogger<EmailNotificationService> logger, IConfiguration configuration) : IEmailNotificationService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var provider = configuration["Email:Provider"] ?? "Simulation";
        if (provider.Equals("Simulation", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[SIMULATION] Email vers {Email}: {Subject} - {Body}", to, subject, body);
            return;
        }

        var host = configuration["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP non configure. Email non envoye vers {Email}", to);
            return;
        }

        using var client = new SmtpClient(host, int.TryParse(configuration["Email:Smtp:Port"], out var port) ? port : 587)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var ssl) ? ssl : true
        };

        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        var from = configuration["Email:From"] ?? username ?? "noreply@mangotaika.local";
        using var message = new MailMessage(from, to, subject, body)
        {
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        await client.SendMailAsync(message);
        logger.LogInformation("Email envoye vers {Email}: {Subject}", to, subject);
    }
}
