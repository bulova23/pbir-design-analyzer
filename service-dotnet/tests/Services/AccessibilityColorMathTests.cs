using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace ServiceDotnet.Tests.Services;

public sealed class AccessibilityColorMathTests
{
    [Theory]
    [InlineData("#abc", "#AABBCC")]
    [InlineData(" abc123 ", "#ABC123")]
    [InlineData("#AbCdEf", "#ABCDEF")]
    [InlineData("000000", "#000000")]
    public void TryNormalizeHex_ReturnsCanonicalSixDigitHex(string input, string expected)
    {
        Assert.Equal(expected, AccessibilityColorMath.TryNormalizeHex(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12")]
    [InlineData("#1234567")]
    [InlineData("#12GG56")]
    public void TryNormalizeHex_ReturnsNullForMalformedInput(string? input)
    {
        Assert.Null(AccessibilityColorMath.TryNormalizeHex(input));
    }

    [Theory]
    [InlineData("#FF0000", "#00FF00", true)]
    [InlineData("#00FF00", "#FF0000", true)]
    [InlineData("#0000FF", "#FFFF00", false)]
    [InlineData("#FF0000", "#F00010", false)]
    public void LooksLikeRedGreenPair_UsesExistingDominanceRule(string first, string second, bool expected)
    {
        Assert.Equal(expected, AccessibilityColorMath.LooksLikeRedGreenPair(first, second));
    }

    [Theory]
    [InlineData("#FF0000", true)]
    [InlineData("#00FF00", false)]
    [InlineData("#282828", false)]
    public void DominanceChecksPreserveChannelBoundaryBehavior(string hex, bool redDominant)
    {
        Assert.Equal(redDominant, AccessibilityColorMath.IsRedDominant(hex));
        Assert.Equal(!redDominant && hex == "#00FF00", AccessibilityColorMath.IsGreenDominant(hex));
    }

    [Fact]
    public void DeuteranopiaSimulationPreservesSimilarityThreshold()
    {
        Assert.True(AccessibilityColorMath.SimulatesToSimilarUnderDeuteranopia("#FF0000", "#F00010"));
        Assert.False(AccessibilityColorMath.SimulatesToSimilarUnderDeuteranopia("#000000", "#FFFFFF"));
    }

    [Fact]
    public void HexToRgbPreservesChannels()
    {
        Assert.Equal((255, 128, 0), AccessibilityColorMath.HexToRgb("#FF8000"));
    }
}
