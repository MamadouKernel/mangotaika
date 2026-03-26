using MangoTaika.Services;

namespace MangoTaika.Tests.Infrastructure;

public sealed class FakeGeocodingService : IGeocodingService
{
    public Task<(double? Lat, double? Lng)> GeocodeAsync(string adresse)
        => Task.FromResult<(double? Lat, double? Lng)>((5.3364, -4.0267));
}
