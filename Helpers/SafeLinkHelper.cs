namespace MangoTaika.Helpers;

public static class SafeLinkHelper
{
    public static string? NormalizeAllowedLink(string? value, bool allowLocal = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (allowLocal && trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal))
            return trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return null;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.ToString();
    }
}
