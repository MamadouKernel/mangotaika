using System.Text.RegularExpressions;
using FluentAssertions;

namespace MangoTaika.Tests.Infrastructure;

internal static class HtmlTestHelpers
{
    public static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html, "meta name=\"request-verification-token\" content=\"([^\"]+)\"");
        match.Success.Should().BeTrue("the shared layout should emit the antiforgery token meta tag");
        return match.Groups[1].Value;
    }
}
