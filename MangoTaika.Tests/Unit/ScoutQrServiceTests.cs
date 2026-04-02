using FluentAssertions;
using MangoTaika.Services;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class ScoutQrServiceTests
{
    [Fact]
    public void GenerateScoutCode_Produces_ReadableSignedCode()
    {
        var provider = DataProtectionProvider.Create("MangoTaika.Tests.ScoutQr");
        var service = new ScoutQrService(provider);
        var scoutId = Guid.NewGuid();

        var code = service.GenerateScoutCode(scoutId);

        code.Should().StartWith("MTSCOUT:");
        service.TryReadScoutId(code, out var decodedScoutId).Should().BeTrue();
        decodedScoutId.Should().Be(scoutId);
    }

    [Fact]
    public void TryReadScoutId_Rejects_InvalidCode()
    {
        var provider = DataProtectionProvider.Create("MangoTaika.Tests.ScoutQr");
        var service = new ScoutQrService(provider);

        service.TryReadScoutId("MTSCOUT:code-invalide", out var decodedScoutId).Should().BeFalse();
        decodedScoutId.Should().Be(Guid.Empty);
    }
}