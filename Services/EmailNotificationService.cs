using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;

namespace MangoTaika.Services;

public class EmailNotificationService(ILogger<EmailNotificationService> logger, IConfiguration configuration) : IEmailNotificationService
{
    public async Task SendAsync(
        string to,
        string subject,
        string body,
        string? recipientName = null,
        string? category = null,
        string? link = null)
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
        var plainText = BuildPlainText(subject, body, recipientName, category, link);
        var html = BuildHtml(subject, body, recipientName, category, link, ResolveLogoUrl());

        using var message = new MailMessage(from, to, subject, plainText)
        {
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html));

        await client.SendMailAsync(message);
        logger.LogInformation("Email envoye vers {Email}: {Subject}", to, subject);
    }

    private static string BuildPlainText(string subject, string body, string? recipientName, string? category, string? link)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName) ? "Bonjour," : $"Bonjour {recipientName},";
        var builder = new StringBuilder();
        builder.AppendLine("MANGO TAIKA - District Scout");
        builder.AppendLine();
        builder.AppendLine(greeting);
        builder.AppendLine();
        builder.AppendLine($"Statut : {subject}");
        if (!string.IsNullOrWhiteSpace(category))
        {
            builder.AppendLine($"Categorie : {category}");
        }
        builder.AppendLine();
        builder.AppendLine(body);
        if (!string.IsNullOrWhiteSpace(link))
        {
            builder.AppendLine();
            builder.AppendLine($"Consulter le dossier : {link}");
        }
        builder.AppendLine();
        builder.AppendLine("Ce message a ete genere automatiquement par la plateforme MANGO TAIKA.");
        return builder.ToString();
    }

    private string ResolveLogoUrl()
    {
        var configuredLogo = configuration["Email:LogoUrl"];
        if (!string.IsNullOrWhiteSpace(configuredLogo))
        {
            return configuredLogo;
        }

        var publicBaseUrl = configuration["App:PublicBaseUrl"]?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(publicBaseUrl)
            ? "https://app.mangotaika.ci/images/logo.png"
            : $"{publicBaseUrl}/images/logo.png";
    }

    private static string BuildHtml(string subject, string body, string? recipientName, string? category, string? link, string logoUrl)
    {
        var encoder = HtmlEncoder.Default;
        var theme = ResolveTheme(category, subject);
        var safeSubject = encoder.Encode(subject);
        var safeBody = ToHtmlParagraphs(body);
        var safeRecipient = encoder.Encode(string.IsNullOrWhiteSpace(recipientName) ? "cher utilisateur" : recipientName);
        var safeCategory = encoder.Encode(string.IsNullOrWhiteSpace(category) ? "Notification" : category);
        var safeLink = string.IsNullOrWhiteSpace(link) ? null : encoder.Encode(link);
        var safePreheader = encoder.Encode(theme.Preheader);
        var safeSectionTitle = encoder.Encode(theme.SectionTitle);
        var safeCtaLabel = encoder.Encode(theme.CtaLabel);
        var safeLogoUrl = encoder.Encode(logoUrl);
        var generatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm 'UTC'");

        return $"""
        <!doctype html>
        <html lang="fr">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{safeSubject}</title>
        </head>
        <body style="margin:0;padding:0;background:#eef2f6;font-family:Arial,Helvetica,sans-serif;color:#24313a;">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{safePreheader}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#eef2f6;padding:24px 12px;">
                <tr>
                    <td align="center">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:640px;background:#ffffff;border-radius:18px;overflow:hidden;border:1px solid #dfe7ef;box-shadow:0 16px 40px rgba(31,49,64,0.12);">
                            <tr>
                                <td style="background:{theme.HeaderBackground};padding:28px 30px;color:#ffffff;">
                                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="width:74px;vertical-align:top;">
                                                <img src="{safeLogoUrl}" width="58" height="58" alt="Logo MANGO TAIKA" style="display:block;width:58px;height:58px;border-radius:14px;background:#ffffff;padding:6px;object-fit:contain;">
                                            </td>
                                            <td style="vertical-align:middle;">
                                                <div style="font-size:12px;letter-spacing:2px;text-transform:uppercase;color:#dbe8c8;font-weight:700;">MANGO TAIKA</div>
                                                <div style="font-size:24px;line-height:1.25;font-weight:800;margin-top:6px;">{safeSectionTitle}</div>
                                                <div style="font-size:13px;color:{theme.HeaderSubtleColor};margin-top:8px;">{safePreheader}</div>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:30px;">
                                    <div style="display:inline-block;padding:8px 12px;border-radius:999px;background:{theme.BadgeBackground};color:{theme.BadgeColor};font-size:13px;font-weight:700;margin-bottom:18px;">{safeCategory}</div>
                                    <h1 style="margin:0 0 12px;font-size:24px;line-height:1.25;color:#26343d;">{safeSubject}</h1>
                                    <p style="margin:0 0 20px;font-size:15px;line-height:1.7;color:#536476;">Bonjour <strong style="color:#26343d;">{safeRecipient}</strong>,</p>

                                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 22px;border:1px solid {theme.StatusBorder};border-radius:14px;background:{theme.StatusBackground};">
                                        <tr>
                                            <td style="padding:16px 18px;">
                                                <div style="font-size:12px;text-transform:uppercase;letter-spacing:1px;color:{theme.StatusLabelColor};font-weight:700;">Statut</div>
                                                <div style="font-size:18px;line-height:1.35;color:{theme.StatusTextColor};font-weight:800;margin-top:4px;">{safeSubject}</div>
                                            </td>
                                        </tr>
                                    </table>

                                    <div style="font-size:15px;line-height:1.7;color:#394956;margin-bottom:24px;">
                                        {safeBody}
                                    </div>

                                    {(safeLink is null ? "" : $"""
                                    <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 26px;">
                                        <tr>
                                            <td style="background:{theme.CtaBackground};border-radius:12px;">
                                                <a href="{safeLink}" style="display:inline-block;padding:13px 20px;color:#ffffff;text-decoration:none;font-weight:800;font-size:14px;">{safeCtaLabel}</a>
                                            </td>
                                        </tr>
                                    </table>
                                    """)}

                                    <div style="border-top:1px solid #e5ebf0;padding-top:18px;font-size:12px;line-height:1.6;color:#7a8794;">
                                        Message genere le {generatedAt}. Si vous n'etes pas concerne par cette notification, vous pouvez l'ignorer.
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td style="background:#26343d;padding:18px 30px;color:#dbe3ea;font-size:12px;line-height:1.6;">
                                    <strong style="color:#ffffff;">MANGO TAIKA - District Scout</strong><br>
                                    Plateforme de gestion scoute. Message automatique, merci de ne pas repondre directement a cet email.
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static EmailTheme ResolveTheme(string? category, string subject)
    {
        var key = $"{category} {subject}".ToLowerInvariant();

        if (key.Contains("rejet") || key.Contains("revision") || key.Contains("reviser"))
        {
            return new EmailTheme(
                "Action requise sur votre dossier MANGO TAIKA",
                "Suivi de demande",
                "Ouvrir la demande",
                "linear-gradient(135deg,#7a2e24,#b35a3c)",
                "#ffe9df",
                "#fff1ec",
                "#8c3a26",
                "#f8ddd4",
                "#fdf7f4",
                "#9b4a33",
                "#6e2d21",
                "#8c3a26");
        }

        if (key.Contains("support") || key.Contains("ticket"))
        {
            return new EmailTheme(
                "Mise a jour de votre ticket support",
                "Support MANGO TAIKA",
                "Ouvrir le ticket",
                "linear-gradient(135deg,#243447,#35637a)",
                "#dceefa",
                "#e9f5fb",
                "#27556b",
                "#d8e8ef",
                "#f5fbfe",
                "#4d7485",
                "#244c5e",
                "#315f77");
        }

        if (key.Contains("formation") || key.Contains("lms") || key.Contains("forum"))
        {
            return new EmailTheme(
                "Information formation disponible",
                "Formations MANGO TAIKA",
                "Ouvrir la formation",
                "linear-gradient(135deg,#28455c,#547a9b)",
                "#e4f0fb",
                "#edf6ff",
                "#315f86",
                "#d9e8f6",
                "#f7fbff",
                "#547596",
                "#284e74",
                "#3d6f9c");
        }

        if (key.Contains("activite") || key.Contains("activites"))
        {
            return new EmailTheme(
                "Information activite a consulter",
                "Activites & suivi",
                "Ouvrir l'activite",
                "linear-gradient(135deg,#31502c,#739441)",
                "#edf7db",
                "#eef7df",
                "#557332",
                "#dae8c8",
                "#f8fbf3",
                "#6c7f58",
                "#314323",
                "#597537");
        }

        return new EmailTheme(
            "Notification officielle de la plateforme",
            "District Scout",
            "Ouvrir le dossier",
            "linear-gradient(135deg,#2f4328,#597537)",
            "#edf5e3",
            "#eaf4dc",
            "#4f702e",
            "#dfe8d3",
            "#f8fbf3",
            "#6c7f58",
            "#314323",
            "#597537");
    }

    private static string ToHtmlParagraphs(string value)
    {
        var encoder = HtmlEncoder.Default;
        var paragraphs = value
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => encoder.Encode(line.Trim()))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (paragraphs.Count == 0)
        {
            return "<p style=\"margin:0;\">Vous avez une nouvelle notification.</p>";
        }

        return string.Join("", paragraphs.Select(line => $"<p style=\"margin:0 0 12px;\">{line}</p>"));
    }

    private sealed record EmailTheme(
        string Preheader,
        string SectionTitle,
        string CtaLabel,
        string HeaderBackground,
        string HeaderSubtleColor,
        string BadgeBackground,
        string BadgeColor,
        string StatusBorder,
        string StatusBackground,
        string StatusLabelColor,
        string StatusTextColor,
        string CtaBackground);
}
