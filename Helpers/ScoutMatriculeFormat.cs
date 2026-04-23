using System.Text.RegularExpressions;

namespace MangoTaika.Helpers;

public static partial class ScoutMatriculeFormat
{
    public const string Example = "0583753X";
    public const string Pattern = @"^\s*\d{7}[A-Za-z]\s*$";
    public const string ListPattern = @"^\s*\d{7}[A-Za-z](\s*,\s*\d{7}[A-Za-z])*\s*$";
    public const string ErrorMessage = "Le matricule doit respecter le format 0583753X.";
    public const string ListErrorMessage = "Chaque matricule doit respecter le format 0583753X.";

    [GeneratedRegex(Pattern, RegexOptions.CultureInvariant)]
    private static partial Regex MatriculeRegex();

    public static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    public static string? NormalizeOptional(string? value)
    {
        var normalized = Normalize(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MatriculeRegex().IsMatch(Normalize(value));

    public static bool TryParseParts(string? value, out int sequence, out char suffix)
    {
        sequence = 0;
        suffix = default;

        var normalized = NormalizeOptional(value);
        if (normalized is null || !IsValid(normalized))
        {
            return false;
        }

        sequence = int.Parse(normalized[..7]);
        suffix = normalized[7];
        return true;
    }

    public static string Compose(int sequence, char suffix)
    {
        if (sequence < 0 || sequence > 9_999_999)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        var normalizedSuffix = char.ToUpperInvariant(suffix);
        if (!char.IsLetter(normalizedSuffix))
        {
            throw new ArgumentOutOfRangeException(nameof(suffix));
        }

        return $"{sequence:0000000}{normalizedSuffix}";
    }
}
