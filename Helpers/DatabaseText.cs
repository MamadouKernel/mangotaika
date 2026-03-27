using System.Globalization;
using System.Text;

namespace MangoTaika.Helpers;

public static class DatabaseText
{
    public static string NormalizeCaseInsensitiveKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    public static string NormalizeSearchKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    public static bool ContainsNormalized(string? value, string normalizedSearchKey)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearchKey))
        {
            return false;
        }

        return NormalizeSearchKey(value).Contains(normalizedSearchKey, StringComparison.Ordinal);
    }

    public static string ToContainsPattern(string value)
    {
        return $"%{value.Trim()}%";
    }

    public static string ToNormalizedContainsPattern(string value)
    {
        return $"%{NormalizeSearchKey(value)}%";
    }
}
