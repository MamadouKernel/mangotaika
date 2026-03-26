namespace MangoTaika.Services;

public interface IGeocodingService
{
    Task<(double? Lat, double? Lng)> GeocodeAsync(string adresse);
}
