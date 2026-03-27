using FluentAssertions;
using MangoTaika.Helpers;
using MangoTaika.Tests.Infrastructure;
using System.Net;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class HomePagesTests
{
    [Fact]
    public async Task Index_Displays_Platform_Slogan()
    {
        await using var factory = new SupportWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        html.Should().Contain(PlatformBranding.Slogan);
    }
}
