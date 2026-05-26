using System.Globalization;
using System.Text;

namespace MangoTaika.Helpers;

public static class BranchDisplay
{
    public const string DefaultLogoUrl = "/images/logo.png";

    public static string CanonicalLabel(string? brancheNom)
    {
        var normalized = Normalize(brancheNom);

        if (normalized.Contains("OISILLON")) return "OISILLON (4 - 7 ANS)";
        if (normalized.Contains("LOUV")) return "LOUVETEAU (8 - 11 ANS)";
        if (normalized.Contains("ECLAIR")) return "ECLAIREUR (12 - 14 ANS)";
        if (normalized.Contains("CHEMIN")) return "CHEMINOT (15 - 17 ANS)";
        if (normalized.Contains("ROUT")) return "ROUTIER (18 - 21 ANS)";
        if (normalized.Contains("BENEVOLE") || normalized.Contains("ADULTE") || normalized.Contains("ADS")) return "BENEVOLES (+ de 21 ANS)";

        return string.IsNullOrWhiteSpace(brancheNom) ? "BRANCHE" : brancheNom.Trim().ToUpperInvariant();
    }

    public static string Accent(string? brancheNom)
    {
        var normalized = Normalize(brancheNom);

        if (normalized.Contains("OISILLON")) return "#ec4899";
        if (normalized.Contains("LOUV")) return "#eab308";
        if (normalized.Contains("ECLAIR")) return "#16a34a";
        if (normalized.Contains("CHEMIN")) return "#f59e0b";
        if (normalized.Contains("ROUT")) return "#ef4444";
        if (normalized.Contains("BENEVOLE") || normalized.Contains("ADULTE") || normalized.Contains("ADS")) return "#5f7f43";

        return "#5f7f43";
    }

    public static string LogoOrDefault(string? logoUrl)
        => string.IsNullOrWhiteSpace(logoUrl) ? DefaultLogoUrl : logoUrl.Trim();

    private static string Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
