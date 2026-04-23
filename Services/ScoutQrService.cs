using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace MangoTaika.Services;

public interface IScoutQrService
{
    string GenerateScoutCode(Guid scoutId);
    bool TryReadScoutId(string? scannedValue, out Guid scoutId);
}

public sealed class ScoutQrService(IDataProtectionProvider dataProtectionProvider) : IScoutQrService
{
    private const string Prefix = "MTSCOUT:";
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("MangoTaika.ScoutQr.v1");

    public string GenerateScoutCode(Guid scoutId)
    {
        var payload = $"v1|{scoutId:N}";
        var protectedPayload = protector.Protect(payload);
        return Prefix + WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedPayload));
    }

    public bool TryReadScoutId(string? scannedValue, out Guid scoutId)
    {
        scoutId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(scannedValue))
        {
            return false;
        }

        var raw = scannedValue.Trim();

        if (raw.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            raw = raw[Prefix.Length..];
        }

        try
        {
            var protectedPayload = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(raw));
            var payload = protector.Unprotect(protectedPayload);
            var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts.Length == 2
                && string.Equals(parts[0], "v1", StringComparison.Ordinal)
                && Guid.TryParseExact(parts[1], "N", out scoutId);
        }
        catch
        {
            return false;
        }
    }
}