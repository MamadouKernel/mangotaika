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

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MatriculeRegex().IsMatch(Normalize(value));
}
