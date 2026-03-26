using System.Net.Http.Headers;
using System.Text.Json;

namespace MangoTaika.Services;

public class GeocodingService(IHttpClientFactory httpClientFactory) : IGeocodingService
{
    public async Task<(double? Lat, double? Lng)> GeocodeAsync(string adresse)
    {
        if (string.IsNullOrWhiteSpace(adresse))
            return (null, null);

        // Essayer d'abord avec l'adresse complète, puis simplifiée
        var queries = new List<string>
        {
            $"{adresse}, Abidjan, Côte d'Ivoire",
            $"{adresse}, Côte d'Ivoire"
        };

        // Ajouter une version simplifiée (premier mot du quartier + commune)
        var parts = adresse.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            var quartierSimple = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(quartierSimple))
            {
                queries.Add($"{quartierSimple}, {parts[^1]}, Abidjan, Côte d'Ivoire");
            }
            queries.Add($"{parts[^1]}, Abidjan, Côte d'Ivoire");
        }

        foreach (var query in queries)
        {
            var result = await SearchNominatimAsync(query);
            if (result.Lat.HasValue) return result;
        }

        return (null, null);
    }

    private async Task<(double? Lat, double? Lng)> SearchNominatimAsync(string query)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Nominatim");
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MangoTaika", "1.0"));

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement;

            if (results.GetArrayLength() == 0)
                return (null, null);

            var first = results[0];
            var lat = double.Parse(first.GetProperty("lat").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
            var lng = double.Parse(first.GetProperty("lon").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);

            return (lat, lng);
        }
        catch
        {
            return (null, null);
        }
    }
}
