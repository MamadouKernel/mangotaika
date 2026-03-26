using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MangoTaika.Services;

/// <summary>
/// Configuration SMS depuis appsettings.json section "Sms"
/// </summary>
public class SmsSettings
{
    /// <summary>
    /// Provider à utiliser : "Simulation", "OrangeSMS" ou "Twilio"
    /// </summary>
    public string Provider { get; set; } = "Simulation";

    // === Orange SMS API (CI) — envoie vers TOUS les réseaux (Orange, MTN, Moov) ===
    public string OrangeClientId { get; set; } = string.Empty;
    public string OrangeClientSecret { get; set; } = string.Empty;
    public string OrangeSenderAddress { get; set; } = string.Empty; // ex: tel:+2250700000000
    public string OrangeAuthUrl { get; set; } = "https://api.orange.com/oauth/v1/token";
    public string OrangeApiUrl { get; set; } = "https://api.orange.com/smsmessaging/v1/outbound";

    // === Twilio (international) — envoie vers TOUS les réseaux, tous les pays ===
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioFromNumber { get; set; } = string.Empty; // ex: +1234567890
}

/// <summary>
/// Service SMS production-ready multi-provider.
/// Supporte Orange SMS API (CI) et Twilio — les deux envoient vers TOUS les réseaux.
/// 
/// En production : changer "Provider" et renseigner les clés dans appsettings.json.
/// Aucun code à modifier.
/// 
/// Providers disponibles :
///   - "Simulation"  → log en console (développement)
///   - "OrangeSMS"   → Orange SMS API CI (envoie vers Orange, MTN, Moov, etc.)
///   - "Twilio"      → Twilio (envoie vers tous les opérateurs, tous les pays)
/// </summary>
public class SmsService(
    ILogger<SmsService> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ISmsService
{
    private readonly SmsSettings _settings = configuration.GetSection("Sms").Get<SmsSettings>() ?? new SmsSettings();

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        switch (_settings.Provider)
        {
            case "OrangeSMS":
                await SendViaOrangeAsync(phoneNumber, message);
                break;

            case "Twilio":
                await SendViaTwilioAsync(phoneNumber, message);
                break;

            default: // Simulation
                logger.LogWarning("═══════════════════════════════════════════");
                logger.LogWarning("📱 [SIMULATION] SMS vers {Phone} : {Message}", phoneNumber, message);
                logger.LogWarning("═══════════════════════════════════════════");
                break;
        }
    }

    // =============================================
    // ORANGE SMS API — Côte d'Ivoire
    // Envoie vers TOUS les réseaux : Orange, MTN, Moov
    // Doc : https://developer.orange.com/apis/sms-ci
    // =============================================

    private async Task SendViaOrangeAsync(string phoneNumber, string message)
    {
        try
        {
            var client = httpClientFactory.CreateClient("OrangeSMS");

            var token = await GetOrangeTokenAsync(client);
            if (string.IsNullOrEmpty(token))
            {
                logger.LogError("❌ Impossible d'obtenir le token Orange SMS. SMS non envoyé vers {Phone}", phoneNumber);
                return;
            }

            var formattedNumber = FormatPhoneNumberCI(phoneNumber);
            var senderEncoded = Uri.EscapeDataString(_settings.OrangeSenderAddress);
            var url = $"{_settings.OrangeApiUrl}/{senderEncoded}/requests";

            var payload = new
            {
                outboundSMSMessageRequest = new
                {
                    address = formattedNumber,
                    senderAddress = _settings.OrangeSenderAddress,
                    outboundSMSTextMessage = new { message }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                logger.LogInformation("✅ SMS envoyé à {Phone} via Orange API", phoneNumber);
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError("❌ Erreur Orange SMS ({Status}) : {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Exception Orange SMS vers {Phone}", phoneNumber);
        }
    }

    private async Task<string?> GetOrangeTokenAsync(HttpClient client)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.OrangeClientId}:{_settings.OrangeClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.OrangeAuthUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    // =============================================
    // TWILIO — International (tous réseaux, tous pays)
    // Doc : https://www.twilio.com/docs/sms
    // =============================================

    private async Task SendViaTwilioAsync(string phoneNumber, string message)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Twilio");
            var formattedNumber = FormatPhoneNumberCI(phoneNumber).Replace("tel:", "");
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.TwilioAccountSid}/Messages.json";

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.TwilioAccountSid}:{_settings.TwilioAuthToken}"));

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("To", formattedNumber),
                new KeyValuePair<string, string>("From", _settings.TwilioFromNumber),
                new KeyValuePair<string, string>("Body", message)
            ]);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                logger.LogInformation("✅ SMS envoyé à {Phone} via Twilio", phoneNumber);
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError("❌ Erreur Twilio ({Status}) : {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Exception Twilio vers {Phone}", phoneNumber);
        }
    }

    // =============================================
    // Utilitaire : formatage numéro CI
    // =============================================

    /// <summary>
    /// Formate un numéro ivoirien vers le format international.
    /// 0701020304 → tel:+2250701020304
    /// Fonctionne quel que soit l'opérateur (Orange, MTN, Moov).
    /// </summary>
    private static string FormatPhoneNumberCI(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith('0'))
            digits = "225" + digits[1..];
        else if (!digits.StartsWith("225"))
            digits = "225" + digits;

        return $"tel:+{digits}";
    }
}
